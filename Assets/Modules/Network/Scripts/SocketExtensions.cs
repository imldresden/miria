// <copyright file="SocketExtensions.cs" company="Technische Universität Dresden">
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

using System.Net.Sockets;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// Extension of the <see cref="Socket"/> class.
    /// </summary>
    public static class SocketExtensions
    {
        /// <summary>
        /// Disposes or closes the <see cref="Socket"/>, depending on the platform.
        /// </summary>
        /// <param name="socket">The socket to dispose or close.</param>
        public static void Kill(this Socket socket)
        {
#if NETFX_CORE
            socket.Dispose();
#else
            socket.Close();
#endif
        }
    }
}