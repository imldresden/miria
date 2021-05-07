// ------------------------------------------------------------------------------------
// <copyright file="Server.cs" company="Technische Universität Dresden">
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

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// Delegate for handling network data.
    /// </summary>
    /// <param name="sender">The object raising the event.</param>
    /// <param name="remoteEndPoint">The remote end point of the data transfer.</param>
    /// <param name="data">The network data.</param>
    public delegate void ByteDataHandler(object sender, IPEndPoint remoteEndPoint, byte[] data);

    /// <summary>
    /// Generic class which establishes a socket connection to receive data over the network.
    /// </summary>
    public abstract class Server : IDisposable
    {
        protected int port;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="port">The port the server will be listening on.</param>
        public Server(int port)
        {
            this.port = port;
        }

        /// <summary>
        /// Called when the server received data from a client.
        /// </summary>
        public event ByteDataHandler DataReceived;

        /// <summary>
        /// Gets a value indicating whether the receiver is currently listening for incoming data or not.
        /// </summary>
        public abstract bool IsListening { get; }

        /// <summary>
        /// Gets the port the receiver listens for incoming data.
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public virtual void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Start the server, which will try to listen for incoming data on the specified port.
        /// </summary>
        /// <returns><value>true</value> if the server was successfully started, otherwise <value>false</value>.</returns>
        public abstract bool Start();

        /// <summary>
        /// Stops the server if it is currently running, freeing the specified port.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Raises the <see cref="DataReceived"/> event.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point of the data transfer.</param>
        /// <param name="data">The network data.</param>
        protected virtual void OnDataReceived(IPEndPoint remoteEndPoint, byte[] data)
        {
            if (DataReceived != null)
            {
                DataReceived(this, remoteEndPoint, data);
            }
        }
    }
}