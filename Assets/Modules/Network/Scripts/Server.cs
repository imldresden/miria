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
    /// Delegate for the DataReceived event of the Server class.
    /// </summary>
    /// <param name="sender">The object sending the event</param>
    /// <param name="remoteEndPoint">The IP/port from which data was received</param>
    /// <param name="data">The data</param>
    public delegate void ByteDataHandler(object sender, IPEndPoint remoteEndPoint, byte[] data);

    /// <summary>
    /// Abstract class which establishes a socket connection to receive/send data over the network.
    /// </summary>
    public abstract class Server : IDisposable
    {
        protected int port;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="port">The local port to use</param>
        public Server(int port)
        {
            this.port = port;
        }

        /// <summary>
        /// This event is raised when data was received from a remote endpoint.
        /// </summary>
        public event ByteDataHandler DataReceived;

        /// <summary>
        /// Gets a value indicating whether the receiver is currently listening for incoming data or not.
        /// </summary>
        public abstract bool IsListening { get; }

        /// <summary>
        /// Gets the port the receiver listens to for incoming data.
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        /// <summary>
        /// Stops and disposes this server, freeing the used native resources
        /// </summary>
        public virtual void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Start the receiver, which will try to listen for incoming data on the specified port.
        /// </summary>
        /// <returns><value>true</value> if the receiver was successfully started, otherwise <value>false</value>.</returns>
        public abstract bool Start();

        /// <summary>
        /// Stops the receiver if it is currently running, freeing the used natives resources.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Gets called when new data was received.
        /// </summary>
        /// <param name="remoteEndPoint">The IP/port the data was received from</param>
        /// <param name="data">The data</param>
        protected virtual void OnDataReceived(IPEndPoint remoteEndPoint, byte[] data)
        {
            DataReceived?.Invoke(this, remoteEndPoint, data);
        }
    }
}