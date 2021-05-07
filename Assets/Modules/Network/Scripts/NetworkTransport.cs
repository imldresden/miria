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
using IMLD.MixedRealityAnalysis.Core;
using IMLD.MixedRealityAnalysis.Network.Messages;
using Network;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
using System.Linq;
#endif

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// This Unity component serves as a layer between the high-level, application specific <see cref="NetworkManagerJson"/> and the low-level network classes.
    /// </summary>
    public class NetworkTransport : MonoBehaviour
    {
        public ServerTcp Server;

        private ClientTcp client;
        private ServerUdp listener;
        private bool justConnected = false;
        private string announceMessage;
        private int port;
        private ClientUdp announcer;
        private string localIP = "127.0.0.1", broadcastIP = "255.255.255.255", serverName;
        private readonly ConcurrentQueue<MessageContainer> messageQueue = new ConcurrentQueue<MessageContainer>();
        private readonly ConcurrentQueue<Socket> clientConnectionQueue = new ConcurrentQueue<Socket>();
        private readonly Dictionary<IPEndPoint, EndPointState> endPointStates = new Dictionary<IPEndPoint, EndPointState>();

        /// <summary>
        /// Gets a value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

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
        public void StartListening()
        {
            // listen for server announcements on broadcast
            listener.Start();
            Debug.Log("searching for server...");
        }

        /// <summary>
        /// Stops listening for servers.
        /// </summary>
        public void StopListening()
        {
            listener.Stop();
        }

        /// <summary>
        /// Connects to a server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        public void ConnectToServer(string ip, int port)
        {
            client = new ClientTcp(ip, port);
            Debug.Log("Connecting to server at " + ip);
            client.Connected += OnConnectedToServer;
            client.DataReceived += OnDataReceived;
            client.Open();
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
            Server = new ServerTcp(this.port);
            Server.ClientConnected += OnClientConnected;
            Server.ClientDisconnected += OnClientDisconnected;
            ////Server.DataReceived += OnDataReceived;
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
            announcer = new ClientUdp(broadcastIP, 11338);
            success = announcer.Open();
            if (success == false)
            {
                Debug.Log("Failed to start announcing!");
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
        /// <param name="client">The client to send the message to.</param>
        public void SendToClient(MessageContainer message, Socket client)
        {
            byte[] envelope = message.Serialize();

            Server.SendToClient(client, envelope);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        internal void StopServer()
        {
            if (announcer != null)
            {
                CancelInvoke("AnnounceServer");
                announcer.Close();
                announcer.Dispose();
                announcer = null;
            }

            if (Server != null)
            {
                Server.Stop();
                Server.Dispose();
                Server = null;
            }
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
                Services.NetworkManager().OnConnectedToServer();
            }

            while (!IsPaused && messageQueue.TryDequeue(out MessageContainer Message))
            {
                await Services.NetworkManager().HandleNetworkMessageAsync(Message);
            }

            while (clientConnectionQueue.TryDequeue(out Socket Client))
            {
                Services.NetworkManager().HandleNewClient(Client);
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
            if (announcer.IsOpen)
            {
                var message = new MessageAnnouncement(announceMessage, localIP, serverName, port);
                announcer.Send(message.Pack().Serialize());
            }
            else
            {
                announcer.Open();
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

        var HostName = (from h in hostnames
                      where h.IPInformation.NetworkAdapter.NetworkAdapterId == profile.NetworkAdapter.NetworkAdapterId
                      select h).FirstOrDefault();
        byte? PrefixLength = HostName.IPInformation.PrefixLength;
        IPAddress IP = IPAddress.Parse(HostName.RawName);
        byte[] IPBytes = IP.GetAddressBytes();
        uint Mask = ~(uint.MaxValue >> PrefixLength.Value);
        byte[] maskBytes = BitConverter.GetBytes(Mask);

        byte[] BroadcastIPBytes = new byte[IPBytes.Length];

        for (int i = 0; i < IPBytes.Length; i++)
        {
            BroadcastIPBytes[i] = (byte)(IPBytes[i] | ~maskBytes[IPBytes.Length - (i+1)]);
        }

        // Convert the bytes to IP addresses.
        broadcastIP = new IPAddress(BroadcastIPBytes).ToString();
        localIP = IP.ToString();
        foreach (HostName hostName in NetworkInformation.GetHostNames())
        {
            if (hostName.Type == HostNameType.DomainName)
            {
                serverName = hostName.DisplayName;
                break;
            }
        }
    }
#else

        private void CollectNetworkInfo()
        {
            // 1. get ipv4 addresses
            var ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            // 2. get net mask for local ip
            // get valid interfaces
            var interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(intf => intf.OperationalStatus == OperationalStatus.Up &&
                (intf.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                intf.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

            // find interface with matching ipv4 and get the net mask
            IEnumerable<UnicastIPAddressInformation> netMasks = null;
            foreach (var netInterface in interfaces)
            {
                netMasks = from inf in netInterface.GetIPProperties().UnicastAddresses
                           from ip in ips
                           where inf.Address.Equals(ip)
                           select inf;
                if (netMasks != null)
                {
                    break;
                }
            }

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

                localIP = ip.ToString();
                broadcastIP = new IPAddress(ipBytes).ToString();
            }

            serverName = System.Environment.ExpandEnvironmentVariables("%ComputerName%");
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