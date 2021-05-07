// <copyright file="ServerTcp.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Patrick Reipschläger
// </author>
// <comment>
//      Originally from the Augmented Displays netcode. Used with permission.
// </comment>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// Delegate for the handling of socket events.
    /// </summary>
    /// <param name="sender">The caller raising the event.</param>
    /// <param name="socket">The socket.</param>
    public delegate void SocketEventHandler(object sender, Socket socket);

    /// <summary>
    /// TCP server building on the general <see cref="Server"/> class.
    /// </summary>
    public class ServerTcp : Server
    {
        #region Private Fields

        private readonly int bufferSize;

        private List<Socket> clients;
        private bool isListening;
        private Socket listener;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerTcp"/> class.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="bufferSize">The buffer size in bytes</param>
        public ServerTcp(int port, int bufferSize = 65536)
            : base(port)
        {
            isListening = false;
            this.bufferSize = bufferSize;
            clients = new List<Socket>();
            Clients = new ReadOnlyCollection<Socket>(clients);
        }

        #endregion Constructors

        #region Events

        /// <summary>
        /// Called, whenever a new client connected.
        /// </summary>
        public event SocketEventHandler ClientConnected;

        /// <summary>
        /// Called, whenever a client disconnected.
        /// </summary>
        public event SocketEventHandler ClientDisconnected;

        #endregion Events

        #region Public Properties

        /// <summary>
        /// Gets a read-only list of all currently connected clients.
        /// </summary>
        public ReadOnlyCollection<Socket> Clients { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the server is currently running and listening for new connections.
        /// </summary>
        public override bool IsListening
        {
            get { return isListening; }
        }

        /// <summary>
        /// Gets the number of currently connected clients.
        /// </summary>
        public int NumberOfConnections
        {
            get { return clients.Count; }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public override void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Sends data to a client.
        /// </summary>
        /// <param name="client">The client to send the data to.</param>
        /// <param name="data">The network data.</param>
        public void SendToClient(Socket client, byte[] data)
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            args.Completed += Send_Completed;
            try
            {
                if (!client.SendAsync(args))
                {
                    Send_Completed(client, args);
                }
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ERROR, Could not send data to client:\n\t" + e.Message);
                args.Dispose();
            }
        }

        /// <summary>
        /// Start the server, which will try to listen for incoming data on the specified port.
        /// </summary>
        /// <returns><value>true</value> if the server was successfully started, otherwise <value>false</value>.</returns>
        public override bool Start()
        {
            Stop();
            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, port));
                listener.Listen(100);

                var args = new SocketAsyncEventArgs();
                args.Completed += Accept_Completed;
                listener.AcceptAsync(args);
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ERROR, Could not start server:\n\t" + e.Message);
                return false;
            }

            isListening = true;
            return true;
        }

        /// <summary>
        /// Stops the server if it is currently running, freeing the specified port.
        /// </summary>
        public override void Stop()
        {
            if (!isListening)
            {
                return;
            }

            isListening = false;

            foreach (var client in clients)
            {
                client.Kill();
            }

            clients.Clear();
            if (listener != null)
            {
                listener.Kill();
                listener = null;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void Accept_Completed(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                if (args.SocketError != SocketError.OperationAborted)
                {
                    Debug.Log("ServerTcp - ERROR '" + args.SocketError.ToString() + "'");
                }

                return;
            }

            // Prepare for data being transmitted by the newly accepted connection
            var clientArgs = new SocketAsyncEventArgs();
            clientArgs.SetBuffer(new byte[bufferSize], 0, bufferSize);
            clientArgs.Completed += Receive_Completed;
            clients.Add(args.AcceptSocket);
            if (ClientConnected != null)
            {
                ClientConnected(this, args.AcceptSocket);
            }

            try
            {
                // receiveAsync might return synchronous, so we handle that too by calling Receive_Completed manually
                if (!args.AcceptSocket.ReceiveAsync(clientArgs))
                {
                    Receive_Completed(args.AcceptSocket, clientArgs);
                }
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ClientReceive ERROR:\n" + e.Message);
                DisconnectClient(args.AcceptSocket);
            }

            // Continue listening for other connections
            try
            {
                var listener_args = new SocketAsyncEventArgs();
                listener_args.Completed += Accept_Completed;
                listener.AcceptAsync(listener_args);
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - Accept ERROR:\n" + e.Message);
                Stop();
            }
        }

        private void DisconnectClient(Socket client)
        {
            if (!clients.Contains(client))
            {
                return;
            }

            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, client);
            }

            client.Kill();
            clients.Remove(client);
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs args)
        {
            var socket = (Socket)sender;
            if (args.SocketError != SocketError.Success)
            {
                Debug.Log("ServerTcp - Socket ERROR '" + args.SocketError.ToString() + "', connection to client terminated");
                args.Dispose();
                DisconnectClient(socket);
                return;
            }

            // if the connection was terminated at the other side, so terminate this side to
            if (args.BytesTransferred == 0)
            {
                DisconnectClient(socket);
                return;
            }

            byte[] msg = new byte[args.BytesTransferred];
            Array.Copy(args.Buffer, 0, msg, 0, args.BytesTransferred);
            OnDataReceived((IPEndPoint)socket.RemoteEndPoint, msg);
            args.Dispose();

            // Continue receiving data from this socket
            var newArgs = new SocketAsyncEventArgs();
            newArgs.SetBuffer(new byte[bufferSize], 0, bufferSize);
            newArgs.Completed += Receive_Completed;
            try
            {
                // receiveAsync might return synchronous, so we handle that too by calling Receive_Completed manually
                if (!socket.ReceiveAsync(newArgs))
                {
                    Receive_Completed(socket, newArgs);
                }
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ERROR, connection to client closed:\n\t" + e.Message);
                newArgs.Dispose();
                DisconnectClient(socket);
            }
        }

        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Debug.Log("ServerTcp - ERROR sending data to client: " + e.SocketError);
            }

            e.Dispose();
        }

        #endregion Private Methods
    }
}