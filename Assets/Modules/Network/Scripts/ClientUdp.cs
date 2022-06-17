// ------------------------------------------------------------------------------------
// <copyright file="ClientUdp.cs" company="Technische Universität Dresden">
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
    /// A UDP client building on the <see cref="Client"/> class.
    /// </summary>
    public class ClientUdp : Client
    {
        private Socket _client;
        private IPEndPoint _endPoint;
        private bool _isOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientUdp"/> class.
        /// </summary>
        /// <param name="ipAddress">The if address of the server, to which the client should connect.</param>
        /// <param name="port">The port of the server, to which the client should connect.</param>
        public ClientUdp(string ipAddress, int port)
            : base(ipAddress, port)
        {
            _isOpen = false;
            _client = null;
            _endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        }

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        public override bool IsOpen
        {
            get { return _client != null; }
        }

        /// <summary>
        /// Terminate the connection to the server and free all used resources.
        /// </summary>
        public override void Close()
        {
            _isOpen = false;
            if (_client != null)
            {
                _client.Kill();
                _client = null;
            }
        }

        /// <summary>
        /// Tries to open the connection to the server.
        /// Note: If the method return true, this doesn't mean the connection itself was successful, only that the attempt has been started.
        /// </summary>
        /// <returns>true if the connection attempt has been successfully started, otherwise false.</returns>
        public override bool Open()
        {
            var args = new SocketAsyncEventArgs();
            try
            {
                _client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _isOpen = true;
            }
            catch (Exception e)
            {
                Debug.LogError("SenderUdp - ERROR, could not open socket:\n" + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Send the specified data to the server. Only works then the client is connected.
        /// </summary>
        /// <param name="data">The data that should be send.</param>
        /// <returns>true if the data has been send, otherwise false.</returns>
        public override bool Send(byte[] data)
        {
            if (!_isOpen)
            {
                return false;
            }

            var args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            args.RemoteEndPoint = _endPoint;
            try
            {
                _client.SendToAsync(args);
            }
            catch (Exception e)
            {
                Debug.LogError("SenderUdp - ERROR while sending data:\n" + e.Message);
                return false;
            }

            return true;
        }
    }
}