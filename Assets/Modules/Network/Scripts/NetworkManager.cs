// ------------------------------------------------------------------------------------
// <copyright file="NetworkManager.cs" company="Technische Universität Dresden">
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
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Core;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A Unity component that represents a network manager. This class manages network connections, message handlers, and server state.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        /// <summary>
        /// Instance reference for the singleton pattern implementation.
        /// </summary>
        public static NetworkManager Instance { get; private set; }

        /// <summary>
        /// The message string that the server should use when announcing itself over network.
        /// </summary>
        public string AnnounceMessage = "MIRIA";

        /// <summary>
        /// The port that the server should use.
        /// </summary>
        public int Port = 11337;

        /// <summary>
        /// The <see cref="NetworkTransport"/> used by this class.
        /// </summary>
        public NetworkTransport Network;

        /// <summary>
        /// Whether the client should automatically try to connect to the first server it finds.
        /// </summary>
        [Tooltip("When started as a client, should automatically connect to the first server found.")]
        public bool AutomaticallyConnectToServer = true;

        /// <summary>
        /// The list of servers or sessions that are available to connect to.
        /// </summary>
        [HideInInspector]
        public Dictionary<string, SessionInfo> Sessions = new Dictionary<string, SessionInfo>();

        /// <summary>
        /// Gets a value indicating the number of currently connected clients.
        /// </summary>
        public int ClientCounter { get; private set; } = 0;

        private Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>> _messageHandlers;

        /// <summary>
        /// Event raised when connected or disconnected.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Event raised when the list of sessions changes.
        /// </summary>
        public event EventHandler<EventArgs> SessionListChanged;

        /// <summary>
        /// Event raised when a new client connects to the server.
        /// </summary>
        public event EventHandler<NewClientEventArgs> ClientConnected;

        /// <summary>
        /// Gets a value indicating whether the app is currently connected to a server.
        /// </summary>
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the app is currently acting as a server.
        /// </summary>
        public bool IsServer { get; internal set; }

        public SessionInfo Session { get; private set; }

        /// <summary>
        /// Handles a network manage by calling the correct registered message handler base on the <see cref="MessageContainer.MessageType"/>.
        /// </summary>
        /// <param name="message">The network message.</param>
        /// <returns>A task object.</returns>
        public async Task HandleNetworkMessageAsync(MessageContainer message)
        {
            if (_messageHandlers != null)
            {
                Func<MessageContainer, Task> callback;
                if (_messageHandlers.TryGetValue(message.Type, out callback) && callback != null)
                {
                    await callback(message);
                }
                else
                {
                    Debug.Log("Unknown message: " + message.Type.ToString() + " with content: " + message.Payload);
                }
            }
        }

        /// <summary>
        /// Handles a new client. All relevant data is sent to the client to get it up to speed.
        /// </summary>
        /// <param name="clientToken">The new client.</param>
        internal void HandleNewClient(Guid clientToken)
        {
            if (!IsServer || Network == null)
            {
                return;
            }

            // assign id to client
            var clientMessage = new MessageAcceptClient(ClientCounter++);
            Network.SendToClient(clientMessage.Pack(), clientToken);

            // notify about the new client
            ClientConnected?.Invoke(this, new NewClientEventArgs(clientToken));
        }

        /// <summary>
        /// Joins the session matching the provided <see cref="SessionInfo"/>.
        /// </summary>
        /// <param name="session">The session that should be joined.</param>
        public void JoinSession(SessionInfo session)
        {
            if (Sessions.TryGetValue(session.SessionIp, out _) == true)
            {
                if (Network != null)
                {
                    IsServer = false;
                    Network.StopServer();
                    Network.StopListening();
                    Network.ConnectToServer(session.SessionIp, session.SessionPort);
                    Session = session;
                }
            }
        }

        /// <summary>
        /// Called when a connection to a server has been established.
        /// </summary>
        public void OnConnectedToNetworkServer()
        {
            IsConnected = true;
            ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Pauses the network. Should be used whenever handling new messages could cause inconsistencies.
        /// Use <see cref="Unpause"/> to restart handling of messages.
        /// </summary>
        public void Pause()
        {
            Network.Pause();
        }

        /// <summary>
        /// Registers a new message handler. There can only be one message handler per message type.
        /// </summary>
        /// <param name="messageType">The type of the message that should be handled.</param>
        /// <param name="messageHandler">A <see cref="Func{MessageContainer, Task}"/> to handle the message.</param>
        /// <returns><see langword="true"/> if the message handler was successfully added, <see langword="false"/> otherwise.</returns>
        public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Func<MessageContainer, Task> messageHandler)
        {
            try
            {
                _messageHandlers[messageType] = messageHandler;
            }
            catch (Exception exp)
            {
                Debug.LogError("Registering message handler failed! Original error message: " + exp.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends a message over network.
        /// The message is sent to the server if coming from a client and it is sent to all clients if coming from a server.
        /// </summary>
        /// <param name="message">The network message.</param>
        public void SendMessage(MessageContainer message)
        {
            if (IsServer)
            {
                Network.SendToAll(message);
            }
            else if (IsConnected)
            {
                Network.SendToServer(message);
            }
        }

        /// <summary>
        /// Sends a message over network.
        /// The message is sent to the server if coming from a client and it is sent to all clients if coming from a server.
        /// </summary>
        /// <param name="message">The network message.</param>
        public void SendMessage(IMessage message)
        {
            SendMessage(message.Pack());
        }

        /// <summary>
        /// Sends a message over network to a specific client.
        /// The message is sent to the client with the given Guid.
        /// </summary>
        /// <param name="message">The network message.</param>
        public void SendMessageToClient(MessageContainer message, Guid clientToken)
        {
            if (IsServer)
            {
                Network.SendToClient(message, clientToken);
            }
        }

        /// <summary>
        /// Tries to start the NetworkManager as a client.
        /// </summary>
        /// <returns><see langword="true"/> if successfully started as a client, <see langword="false"/> otherwise.</returns>
        public bool StartAsClient()
        {
            if (!enabled)
            {
                Debug.Log("Network Manager disabled, cannot start client!");
                return false;
            }

            if (!Network || Network.enabled == false)
            {
                Debug.Log("Network transport not ready, cannot start client!");
                return false;
            }

            Debug.Log("Starting as client");
            IsServer = false;
            bool Success = Network.StartListening();
            return Success;
        }

        /// <summary>
        /// Tries to start the NetworkManager as a server.
        /// </summary>
        /// <returns><see langword="true"/> if successfully started as a server, <see langword="false"/> otherwise.</returns>
        public bool StartAsServer()
        {
            if (!enabled)
            {
                Debug.Log("Network Manager disabled, cannot start server!");
                return false;
            }

            if (!Network || Network.enabled == false)
            {
                Debug.Log("Network transport not ready, cannot start server!");
                return false;
            }

            Debug.Log("Starting as server");
            bool Success = Network.StartServer(Port, AnnounceMessage);
            if (Success)
            {
                Network.StopListening();
                IsServer = true;
                ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
            }
            return Success;
        }

        /// <summary>
        /// Restarts the network manager after it has been paused. See also <see cref="Pause"/>.
        /// </summary>
        public void Unpause()
        {
            Network.Unpause();
        }

        /// <summary>
        /// Removes a registered message handler.
        /// </summary>
        /// <param name="messageType">The message type for which the message handler should be cleared.</param>
        /// <returns><see langword="true"/> if the message handler was successfully removed, <see langword="false"/> otherwise.</returns>
        public bool UnregisterMessageHandler(MessageContainer.MessageType messageType)
        {
            return _messageHandlers.Remove(messageType);
        }

        private void Awake()
        {
            _messageHandlers = new Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>>();

            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private Task OnBroadcastData(MessageContainer obj)
        {
            Debug.Log("Received broadcast!");
            MessageAnnouncement message = MessageAnnouncement.Unpack(obj); // deserialize message

            // check if the announcement strings matches
            if (message != null && message.Message.Equals(AnnounceMessage))
            {
                SessionInfo sessionInfo;
                if (Sessions.TryGetValue(message.IP, out sessionInfo) == false)
                {
                    // add to session list
                    sessionInfo = new SessionInfo() { SessionName = message.Name, SessionIp = message.IP, SessionPort = message.Port };
                    Sessions.Add(message.IP, sessionInfo);
                    // trigger event to notify about new session
                    SessionListChanged?.Invoke(this, EventArgs.Empty);
                    if (AutomaticallyConnectToServer == true)
                    {
                        JoinSession(sessionInfo);
                    }
                }
            }

            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        private void Start()
        {
            // registers callback for announcement handling
            RegisterMessageHandler(MessageContainer.MessageType.ANNOUNCEMENT, OnBroadcastData);

            if (Network)
            {
                StartAsClient();
            }
        }

        /// <summary>
        /// Stores information about a server session.
        /// </summary>
        public class SessionInfo
        {
            public string SessionIp;
            public string SessionName;
            public int SessionPort;
        }

        public class NewClientEventArgs : EventArgs
        {
            public NewClientEventArgs(Guid clientToken)
            {
                ClientToken = clientToken;
            }
            public Guid ClientToken { get; }
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        public class CustomSerializationBinder : ISerializationBinder
        {
            private static readonly Regex regex = new Regex(@"System\.Private\.CoreLib(, Version=[\d\.]+)?(, Culture=[\w-]+)(, PublicKeyToken=[\w\d]+)?");
            public Type BindToType(string assemblyName, string typeName)
            {
                if (assemblyName.Contains("System.Private.CoreLib"))
                    assemblyName = assemblyName.Replace("System.Private.CoreLib", "mscorlib");

                if (typeName.Contains("System.Private.CoreLib"))
                    typeName = typeName.Replace("System.Private.CoreLib", "mscorlib");

                return new DefaultSerializationBinder().BindToType(assemblyName, typeName);
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                new DefaultSerializationBinder().BindToName(serializedType, out assemblyName, out typeName);

                if (assemblyName.Contains("System.Private.CoreLib"))
                    assemblyName = regex.Replace(assemblyName, "mscorlib");

                if (typeName.Contains("System.Private.CoreLib"))
                    typeName = regex.Replace(typeName, "mscorlib");
            }
        }

#endif

    }
}