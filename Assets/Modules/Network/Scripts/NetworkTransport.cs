// ------------------------------------------------------------------------------------
// <copyright file="NetworkTransport.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// This Unity component serves as a layer between the high-level, application specific <see cref="NetworkManager"/> and the low-level network classes.
    /// </summary>
    public class NetworkTransport : MonoBehaviour
    {
        private ServerTcp server;
        private ClientTcp client;
        private ServerUdp listener;
        private bool justConnected = false;
        private string announceMessage;
        private int port;
        private readonly List<ClientUdp> announcers = new List<ClientUdp>();
        private string serverName = "Server";
        private readonly ConcurrentQueue<MessageContainer> messageQueue = new ConcurrentQueue<MessageContainer>();
        private readonly ConcurrentQueue<Socket> clientConnectionQueue = new ConcurrentQueue<Socket>();
        private readonly Dictionary<IPEndPoint, EndPointState> endPointStates = new Dictionary<IPEndPoint, EndPointState>();
        private readonly Dictionary<string, string> broadcastIPs = new Dictionary<string, string>();

        /// <summary>
        /// Gets a value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (client != null && client.IsOpen)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the port that the server is running on.
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        /// <summary>
        /// Gets the server name.
        /// </summary>
        public string ServerName
        { 
            get { return serverName; }
        }

        /// <summary>
        /// Gets the server IPs.
        /// </summary>
        public IReadOnlyList<string> ServerIPs
        {
            get { return broadcastIPs.Values.ToList().AsReadOnly(); }
        }

        /// <summary>
        /// Pauses the handling of network messages.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Restarts the handling of network messages.
        /// </summary>
        public void Unpause()
        {
            IsPaused = false;
        }

        /// <summary>
        /// Starts listening for servers.
        /// </summary>
        /// <returns><see langword="true"/> if the client started listening for announcements, <see langword="false"/> otherwise.</returns>
        public bool StartListening()
        {
            // listen for server announcements on broadcast
            Debug.Log("searching for server...");
            return listener.Start();
        }

        /// <summary>
        /// Stops listening for servers.
        /// </summary>
        public void StopListening()
        {
            listener?.Stop();
        }

        /// <summary>
        /// Connects to a server.
        /// Note: If the method return true, this doesn't mean the connection itself was successful, only that the attempt has been started.
        /// Subscribe to the OnConnectedToServer event to be notified if the connection to the server has been successfully established.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <returns><see langword="true"/> if the connection attempt was started successful, <see langword="false"/> otherwise.</returns>
        public bool ConnectToServer(string ip, int port)
        {
            client = new ClientTcp(ip, port);
            Debug.Log("Connecting to server at " + ip);
            client.Connected += OnConnectedToServer;
            client.DataReceived += OnDataReceived;
            return client.Open();
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(MessageContainer message)
        {
            client.Send(message.Serialize());
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="message">The message to announce the server with.</param>
        /// <returns><see langword="true"/> if the server started successfully, <see langword="false"/> otherwise.</returns>
        public bool StartServer(int port, string message)
        {
            this.port = port;
            announceMessage = message;

            // setup server
            server = new ServerTcp(this.port);
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            ////Server.DataReceived += OnDataReceived;
            server.DataReceived += OnDataReceivedAtServer;

            // start server
            bool success = server.Start();
            if (success == false)
            {
                Debug.Log("Failed to start server!");
                return false;
            }

            Debug.Log("Started server!");

            // announce server via broadcast
            success = false;
            foreach (var item in broadcastIPs)
            {
                var announcer = new ClientUdp(item.Key, 11338);
                if (!announcer.Open())
                {
                    Debug.Log("Failed to start announcing on " + item.Key + "!");
                }
                else
                {
                    announcers.Add(announcer);
                    Debug.Log("Started announcing on " + item.Key + "!");
                    success = true;
                }
            }

            if (success == false)
            {
                Debug.LogError("Failed to start announcing server!");
                return false;
            }

            InvokeRepeating(nameof(AnnounceServer), 1.0f, 2.0f);
            return true;
        }

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToAll(MessageContainer message)
        {
            byte[] envelope = message.Serialize();
            foreach (var client in server.Clients)
            {
                if (client.Connected)
                {
                    server.SendToClient(client, envelope);
                }
            }
        }

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="client">The client to send the message to.</param>
        public void SendToClient(MessageContainer message, Socket client)
        {
            byte[] envelope = message.Serialize();

            server.SendToClient(client, envelope);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            if (announcers?.Count != 0)
            {
                CancelInvoke("AnnounceServer");
                foreach (var announcer in announcers)
                {
                    announcer.Close();
                    announcer.Dispose();
                }

                announcers.Clear();
            }

            server?.Stop();
            server?.Dispose();
            server = null;
        }

        private void Awake()
        {
            // compute local & broadcast ip and look up server name
            CollectNetworkInfo(); // platform dependent, might not work in all configurations

            // create listen server for server announcements
            listener = new ServerUdp(11338);
            listener.DataReceived += OnBroadcastDataReceived;
        }

        private async void Update()
        {
            if (justConnected)
            {
                justConnected = false;
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.OnConnectedToServer();
                }
            }

            MessageContainer message;
            while (!IsPaused && messageQueue.TryDequeue(out message))
            {
                if (NetworkManager.Instance != null)
                {
                    await NetworkManager.Instance.HandleNetworkMessageAsync(message);
                }
            }

            Socket client;
            while (clientConnectionQueue.TryDequeue(out client))
            {
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.HandleNewClient(client);
                }
            }
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            Debug.Log("Connected to server!");
            justConnected = true;
        }

        private void OnBroadcastDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            messageQueue.Enqueue(MessageContainer.Deserialize(remoteEndPoint, data));
        }

        // called by InvokeRepeating
        private void AnnounceServer()
        {
            foreach (var announcer in announcers)
            {
                if (announcer.IsOpen)
                {
                    var message = new MessageAnnouncement(announceMessage, broadcastIPs[announcer.IpAddress], serverName, port);
                    announcer.Send(message.Pack().Serialize());
                }
                else
                {
                    announcer.Open();
                }
            }
        }

        private void OnDataReceivedAtServer(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            // dispatch received data to all other clients (but not the original sender)
            if (server != null)
            {
                // only if we have a server
                Dispatch(remoteEndPoint, data);
            }

            OnDataReceived(sender, remoteEndPoint, data);
        }

        private void OnDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            int currentByte = 0;
            int dataLength = data.Length;
            EndPointState state;
            try
            {
                if (endPointStates.ContainsKey(remoteEndPoint))
                {
                    state = endPointStates[remoteEndPoint];
                }
                else
                {
                    state = new EndPointState();
                    endPointStates[remoteEndPoint] = state;
                }

                state.CurrentSender = remoteEndPoint;
                while (currentByte < dataLength)
                {
                    int messageSize;

                    // currently still reading a (large) message?
                    if (state.IsMessageIncomplete)
                    {
                        // 1. get size of current message
                        messageSize = state.CurrentMessageBuffer.Length;

                        // 2. read data
                        // decide how much to read: not more than remaining message size, not more than remaining data size
                        int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, data.Length - currentByte);
                        Array.Copy(data, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                        currentByte += lengthToRead; // increase "current byte pointer"
                        state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

                        // 3. decide how to proceed
                        if (state.CurrentMessageBytesRead == messageSize)
                        {
                            // Message is completed
                            state.IsMessageIncomplete = false;
                            messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
                        }
                        else
                        {
                            // We did not read the whole message yet
                            state.IsMessageIncomplete = true;
                        }
                    }
                    else if (state.IsHeaderIncomplete)
                    {
                        // currently still reading a header
                        // decide how much to read: not more than remaining message size, not more than remaining header size
                        int lengthToRead = Math.Min(5 - state.CurrentHeaderBytesRead, dataLength - currentByte);
                        Array.Copy(data, currentByte, state.CurrentHeaderBuffer, state.CurrentHeaderBytesRead, lengthToRead); // read header data into header buffer
                        currentByte += lengthToRead;
                        state.CurrentHeaderBytesRead += lengthToRead;
                        if (state.CurrentHeaderBytesRead == 5)
                        {
                            // Message header is completed
                            // read size of message from header buffer
                            messageSize = BitConverter.ToInt32(state.CurrentHeaderBuffer, 0);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;

                            // read type of next message
                            state.CurrentMessageType = state.CurrentHeaderBuffer[4];
                            state.IsHeaderIncomplete = false;
                            state.IsMessageIncomplete = true;
                        }
                        else
                        {
                            // We did not read the whole header yet
                            state.IsHeaderIncomplete = true;
                        }
                    }
                    else
                    {
                        // start reading a new message
                        // 1. check if remaing data sufficient to read message header
                        if (currentByte < dataLength - 5)
                        {
                            // 2. read size of next message
                            messageSize = BitConverter.ToInt32(data, currentByte);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;
                            currentByte += 4;

                            // 3. read type of next message
                            state.CurrentMessageType = data[currentByte];
                            currentByte += 1;

                            // 4. read data
                            // decide how much to read: not more than remaining message size, not more than remaining data size
                            int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, dataLength - currentByte);
                            Array.Copy(data, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                            currentByte += lengthToRead; // increase "current byte pointer"
                            state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

                            // 4. decide how to proceed
                            if (state.CurrentMessageBytesRead == messageSize)
                            {
                                // Message is completed
                                state.IsMessageIncomplete = false;
                                messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
                            }
                            else
                            {
                                // We did not read the whole message yet
                                state.IsMessageIncomplete = true;
                            }
                        }
                        else
                        {
                            // not enough data to read complete header for new message
                            state.CurrentHeaderBuffer = new byte[5]; // create new header data buffer to store a partial message header
                            int lengthToRead = dataLength - currentByte;
                            Array.Copy(data, currentByte, state.CurrentHeaderBuffer, 0, lengthToRead); // read header data into header buffer
                            currentByte += lengthToRead;
                            state.CurrentHeaderBytesRead = lengthToRead;
                            state.IsHeaderIncomplete = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while parsing network data: " + e.Message);
            }
        }

        private void OnDestroy()
        {
            StopListening();
            StopServer();
        }

        private void Dispatch(IPEndPoint sender, byte[] data)
        {
            var clients = new List<Socket>(server.Clients);
            foreach (var client in clients)
            {
                if (sender.Address.ToString().Equals(((IPEndPoint)client.RemoteEndPoint).Address.ToString()))
                {
                    continue;
                }
                else
                {
                    server.SendToClient(client, data);
                }
            }
        }

        private void OnClientDisconnected(object sender, Socket socket)
        {
            Debug.Log("Client disconnected");
        }

        private void OnClientConnected(object sender, Socket socket)
        {
            Debug.Log("Client connected: " + IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()));
            clientConnectionQueue.Enqueue(socket);
        }

#if UNITY_WSA && !UNITY_EDITOR
    private void CollectNetworkInfo()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();

        IEnumerable<HostName> hostnames =
            NetworkInformation.GetHostNames().Where(h =>
                h.IPInformation != null &&
                h.IPInformation.NetworkAdapter != null &&
                h.Type == HostNameType.Ipv4).ToList();

        var hostName = (from h in hostnames
                      where h.IPInformation.NetworkAdapter.NetworkAdapterId == profile.NetworkAdapter.NetworkAdapterId
                      select h).FirstOrDefault();
        byte? prefixLength = hostName.IPInformation.PrefixLength;
        IPAddress ip = IPAddress.Parse(hostName.RawName);
        byte[] ipBytes = ip.GetAddressBytes();
        uint mask = ~(uint.MaxValue >> prefixLength.Value);
        byte[] maskBytes = BitConverter.GetBytes(mask);

        byte[] broadcastIPBytes = new byte[ipBytes.Length];

        for (int i = 0; i < ipBytes.Length; i++)
        {
            broadcastIPBytes[i] = (byte)(ipBytes[i] | ~maskBytes[ipBytes.Length - (i+1)]);
        }

        // Convert the bytes to IP addresses.
        string broadcastIP = new IPAddress(broadcastIPBytes).ToString();
        string localIP = ip.ToString();
        foreach (HostName name in NetworkInformation.GetHostNames())
        {
            if (name.Type == HostNameType.DomainName)
            {
                serverName = name.DisplayName;
                break;
            }
        }
        broadcastIPs.Clear();
        broadcastIPs[broadcastIP] = localIP;
    }
#else

        private void CollectNetworkInfo()
        {
            serverName = System.Environment.ExpandEnvironmentVariables("%ComputerName%");
            broadcastIPs.Clear();

            // 1. get ipv4 addresses
            var ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            // 2. get net mask for local ip
            // get valid interfaces
            var Interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(intf => intf.OperationalStatus == OperationalStatus.Up &&
                (intf.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                intf.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

            // find interface with matching ipv4 and get the net mask
            IEnumerable<UnicastIPAddressInformation> netMasks = null;
            foreach (var Interface in Interfaces)
            {
                netMasks = from inf in Interface.GetIPProperties().UnicastAddresses
                           from IP in ips
                           where inf.Address.Equals(IP)
                           select inf;
                if (netMasks != null)
                {
                    IPAddress netMask = netMasks.FirstOrDefault().IPv4Mask;
                    IPAddress ip = netMasks.FirstOrDefault().Address;
                    byte[] maskBytes = netMask.GetAddressBytes();
                    byte[] ipBytes = ip.GetAddressBytes();
                    for (int i = 0; i < ipBytes.Length; i++)
                    {
                        ipBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                    }

                    string localIP = ip.ToString();
                    string broadcastIP = new IPAddress(ipBytes).ToString();
                    broadcastIPs[broadcastIP] = localIP;
                }
            }
        }

#endif
    }

    /// <summary>
    /// Helper class used to store the current state of a network endpoint.
    /// </summary>
    internal class EndPointState
    {
        public byte[] CurrentMessageBuffer;
        public int CurrentMessageBytesRead;
        public byte CurrentMessageType;
        public bool IsMessageIncomplete = false;
        public IPEndPoint CurrentSender;
        public bool IsHeaderIncomplete = false;
        public byte[] CurrentHeaderBuffer;
        public int CurrentHeaderBytesRead;
    }
}