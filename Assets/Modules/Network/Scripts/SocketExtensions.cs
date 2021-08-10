using System.Net.Sockets;

namespace IMLD.MixedRealityAnalysis.Network
{
    public static class SocketExtensions
    {
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