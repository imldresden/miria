// ------------------------------------------------------------------------------------
// <copyright file="ServerUdp.cs" company="Technische Universität Dresden">
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
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// UDP server class, based on the abstract Server class, which opens a socket connection to receive/send data over the network.
    /// </summary>
    public class ServerUdp : Server
    {
        private readonly int bufferSize;
        private Socket socket;
        private SocketAsyncEventArgs socketEventArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerUdp"/> class.
        /// </summary>
        /// <param name="port">The port for this server.</param>
        /// <param name="bufferSize">The data buffer size in bytes.</param>
        public ServerUdp(int port, int bufferSize = 65536)
            : base(port)
        {
            this.bufferSize = bufferSize;
            socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            socketEventArgs.SetBuffer(new byte[this.bufferSize], 0, this.bufferSize);
            socketEventArgs.Completed += Receive_Completed;
        }

        /// <summary>
        /// Gets a value indicating whether the server is currently running and listening for new connections.
        /// </summary>
        public override bool IsListening
        {
            get { return socket != null; }
        }

        /// <summary>
        /// Start the server, which will try to listen for incoming data on the specified port.
        /// </summary>
        /// <returns><value>true</value> if the server was successfully started, otherwise <value>false</value>.</returns>
        public override bool Start()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
#if !NETFX_CORE
                socket.EnableBroadcast = true;
#endif
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.ReceiveFromAsync(socketEventArgs);
            }
            catch (Exception e)
            {
                Debug.LogError("ReceiverUdp - ERROR, could not open socket:\n" + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Stops the server if it is currently running, freeing the used natives resources.
        /// </summary>
        public override void Stop()
        {
            if (socket == null)
            {
                return;
            }

            socket.Kill();
            socket = null;
        }

        /// <summary>
        /// Stops and disposes this server, freeing the used native resources
        /// </summary>
        public override void Dispose()
        {
            Stop();
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0)
            {
                return;
            }

            byte[] msg = new byte[e.BytesTransferred];
            Array.Copy(e.Buffer, 0, msg, 0, e.BytesTransferred);
            OnDataReceived((IPEndPoint)e.RemoteEndPoint, msg);

            socket.ReceiveFromAsync(e);
        }
    }
}