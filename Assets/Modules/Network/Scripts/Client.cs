// ------------------------------------------------------------------------------------
// <copyright file="Client.cs" company="Technische Universität Dresden">
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

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// Abstract class which establishes a socket connection to send data over the network.
    /// </summary>
    public abstract class Client : IDisposable
    {
        protected string ipAddress;
        protected int port;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="ipAddress">The if address of the server, to which the client should connect.</param>
        /// <param name="port">The port of the server, to which the client should connect.</param>
        public Client(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        /// <summary>
        /// Gets the remote IP address to which the data should be send.
        /// </summary>
        public string IpAddress
        {
            get { return ipAddress; }
        }

        /// <summary>
        /// Gets a value indicating whether the sender is ready to send data to the remote connection.
        /// </summary>
        public abstract bool IsOpen { get; }

        /// <summary>
        /// Gets the remote port to which the data should be send.
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Frees all resources used by this Client.
        /// </summary>
        public virtual void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Tries to open the socket connection and, depending on the used protocol, tries to establish a connection with the remote host.
        /// </summary>
        /// <returns><value>true</value> if the socket was opened successfully, otherwise <value>false</value>.</returns>
        public abstract bool Open();

        /// <summary>
        /// Tries to send the specified data to the remote host.
        /// </summary>
        /// <param name="data">The data which should be send.</param>
        /// <returns><value>true</value> if the sending operation was successful, otherwise <value>false</value>.</returns>
        public abstract bool Send(byte[] data);
    }
}