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
        public ServerTcp Server;
        public int AnnouncementPort = 11338;

        private const int MESSAGE_HEADER_LENGTH = MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH;
        private const int MESSAGE_SIZE_LENGTH = 4;
        private const int MESSAGE_TYPE_LENGTH = 1;

        private ClientTcp _client;
        private ServerUdp _listener;
        private bool _justConnected = false;
        private bool _isConnecting = false;
        private string _announceMessage;
        private int _port;
        private readonly List<ClientUdp> _announcers = new List<ClientUdp>();
        private string _serverName = "Server";
        private readonly ConcurrentQueue<MessageContainer> _messageQueue = new ConcurrentQueue<MessageContainer>();
        private readonly ConcurrentQueue<Guid> _clientConnectionQueue = new ConcurrentQueue<Guid>();
        private readonly Dictionary<IPEndPoint, EndPointState> _endPointStates = new Dictionary<IPEndPoint, EndPointState>();
        private readonly Dictionary<string, string> _broadcastIPs = new Dictionary<string, string>();
        private readonly Dictionary<Guid, Socket> _clientList = new Dictionary<Guid, Socket>();

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
                if (_client != null && _client.IsOpen)
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
            get { return _port; }
        }

        /// <summary>
        /// Gets the server name.
        /// </summary>
        public string ServerName
        { 
            get { return _serverName; }
        }

        /// <summary>
        /// Gets the server IPs.
        /// </summary>
        public IReadOnlyList<string> ServerIPs
        {
            get { return _broadcastIPs.Values.ToList().AsReadOnly(); }
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
            return _listener.Start();
        }

        /// <summary>
        /// Stops listening for servers.
        /// </summary>
        public void StopListening()
        {
            _listener?.Stop();
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
            if (_isConnecting)
            {
                return false;
            }

            if (IsConnected)
            {
                _client.Close();
            }

            _client = new ClientTcp(ip, port);
            Debug.Log("Connecting to server at " + ip);
            _client.Connected += OnConnectedToServer;
            _client.DataReceived += OnDataReceived;
            _isConnecting = true;
            return _client.Open();
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(MessageContainer message)
        {
            _client.Send(message.Serialize());
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="message">The message to announce the server with.</param>
        /// <returns><see langword="true"/> if the server started successfully, <see langword="false"/> otherwise.</returns>
        public bool StartServer(int port, string message)
        {
            _port = port;
            _announceMessage = message;

            // setup server
            Server = new ServerTcp(_port);
            Server.ClientConnected += OnClientConnected;
            Server.ClientDisconnected += OnClientDisconnected;
            Server.DataReceived += OnDataReceivedAtServer;

            // start server
            bool success = Server.Start();
            if (success == false)
            {
                Debug.Log("Failed to start server!");
                return false;
            }

            Debug.Log("Started server!");

            // announce server via broadcast
            success = false;
            foreach (var item in _broadcastIPs)
            {
                var announcer = new ClientUdp(item.Key, AnnouncementPort);
                if (!announcer.Open())
                {
                    Debug.Log("Failed to start announcing on " + item.Key + "!");
                }
                else
                {
                    _announcers.Add(announcer);
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
            foreach (var client in Server.Clients)
            {
                if (client.Connected)
                {
                    Server.SendToClient(client, envelope);
                }
            }
        }

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="clientToken">A Guid representing the client to send the message to.</param>
        public void SendToClient(MessageContainer message, Guid clientToken)
        {
            byte[] envelope = message.Serialize();
            Server.SendToClient(_clientList[clientToken], envelope);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            if (_announcers != null && _announcers.Count != 0)
            {
                CancelInvoke("AnnounceServer");
                foreach (var announcer in _announcers)
                {
                    announcer?.Close();
                    announcer?.Dispose();
                }

                _announcers.Clear();
            }

            Server?.Stop();
            Server?.Dispose();
            Server = null;
        }

        private void Awake()
        {
            // compute local & broadcast ip and look up server name
            CollectNetworkInfo(); // platform dependent, might not work in all configurations

            // create listen server for server announcements
            _listener = new ServerUdp(AnnouncementPort);
            _listener.DataReceived += OnBroadcastDataReceived;
        }

        private async void Update()
        {
            if (_justConnected)
            {
                _justConnected = false;
                NetworkManager.Instance?.OnConnectedToNetworkServer();
            }

            MessageContainer message;
            while (!IsPaused && _messageQueue.TryDequeue(out message))
            {
                await NetworkManager.Instance?.HandleNetworkMessageAsync(message);
            }

            Guid client;
            while (_clientConnectionQueue.TryDequeue(out client))
            {
                NetworkManager.Instance?.HandleNewClient(client);
            }
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            Debug.Log("Connected to server!");
            _justConnected = true;
            _isConnecting = false;
        }

        private void OnBroadcastDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            _messageQueue.Enqueue(MessageContainer.Deserialize(remoteEndPoint, data));
        }

        // called by InvokeRepeating
        private void AnnounceServer()
        {
            foreach (var announcer in _announcers)
            {
                if (announcer.IsOpen)
                {
                    var message = new MessageAnnouncement(_announceMessage, _broadcastIPs[announcer.IpAddress], _serverName, _port);
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
            if (Server != null)
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
                if (_endPointStates.ContainsKey(remoteEndPoint))
                {
                    state = _endPointStates[remoteEndPoint];
                }
                else
                {
                    state = new EndPointState();
                    _endPointStates[remoteEndPoint] = state;
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
                            _messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
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
                        int lengthToRead = Math.Min(MESSAGE_HEADER_LENGTH - state.CurrentHeaderBytesRead, dataLength - currentByte);
                        Array.Copy(data, currentByte, state.CurrentHeaderBuffer, state.CurrentHeaderBytesRead, lengthToRead); // read header data into header buffer
                        currentByte += lengthToRead;
                        state.CurrentHeaderBytesRead += lengthToRead;
                        if (state.CurrentHeaderBytesRead == MESSAGE_HEADER_LENGTH)
                        {
                            // Message header is completed
                            // read size of message from header buffer
                            messageSize = BitConverter.ToInt32(state.CurrentHeaderBuffer, 0);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;

                            // read type of next message
                            state.CurrentMessageType = state.CurrentHeaderBuffer[MESSAGE_SIZE_LENGTH];
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
                        // 1. check if remaining data sufficient to read message header
                        if (currentByte < dataLength - MESSAGE_HEADER_LENGTH)
                        {
                            // 2. read size of next message
                            messageSize = BitConverter.ToInt32(data, currentByte);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;
                            currentByte += MESSAGE_SIZE_LENGTH;

                            // 3. read type of next message
                            state.CurrentMessageType = data[currentByte];
                            currentByte += MESSAGE_TYPE_LENGTH;

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
                                _messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
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
                            state.CurrentHeaderBuffer = new byte[MESSAGE_HEADER_LENGTH]; // create new header data buffer to store a partial message header
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
            var clients = new List<Socket>(Server.Clients);
            foreach (var client in clients)
            {
                if (sender.Address.ToString().Equals(((IPEndPoint)client.RemoteEndPoint).Address.ToString()))
                {
                    continue;
                }
                else
                {
                    Server.SendToClient(client, data);
                }
            }
        }

        private void OnClientDisconnected(object sender, Socket socket)
        {
            var guid = _clientList?.FirstOrDefault(x => x.Value == socket).Key;
            if(guid != null)
            {
                _clientList?.Remove((Guid)guid);
            }            
            Debug.Log("Client disconnected");
        }

        private void OnClientConnected(object sender, Socket socket)
        {
            Debug.Log("Client connected: " + IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()));
            Guid guid = Guid.NewGuid();
            _clientList?.Add(guid, socket);
            _clientConnectionQueue.Enqueue(guid);
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
                _serverName = name.DisplayName;
                break;
            }
        }
        _broadcastIPs.Clear();
        _broadcastIPs[broadcastIP] = localIP;
    }
#else

        private void CollectNetworkInfo()
        {
            _serverName = Environment.ExpandEnvironmentVariables("%ComputerName%");
            _broadcastIPs.Clear();

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
                    _broadcastIPs[broadcastIP] = localIP;
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