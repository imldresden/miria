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
        public static NetworkManager Instance = null;

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
        /// The list of servers or sessions that are available to connect to.
        /// </summary>
        [HideInInspector]
        public Dictionary<string, SessionInfo> Sessions = new Dictionary<string, SessionInfo>();

        private int clientCounter = 0;

        private Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>> messageHandlers;

        /// <summary>
        /// Event raised when connected or disconnected.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Event raised when the list of sessions changes.
        /// </summary>
        public event EventHandler<EventArgs> SessionListChanged;

        /// <summary>
        /// Gets a value indicating whether the app is currently connected to a server.
        /// </summary>
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the app is currently acting as a server.
        /// </summary>
        public bool IsServer { get; internal set; }

        /// <summary>
        /// Handles a network manage by calling the correct registered message handler base on the <see cref="MessageContainer.MessageType"/>.
        /// </summary>
        /// <param name="message">The network message.</param>
        /// <returns>A task object.</returns>
        public async Task HandleNetworkMessageAsync(MessageContainer message)
        {
            if (messageHandlers != null)
            {
                if (messageHandlers.TryGetValue(message.Type, out Func<MessageContainer, Task> Callback) && Callback != null)
                {
                    await Callback(message);
                }
                else
                {
                    Debug.Log("Unknown message: " + message.Type.ToString() + " with content: " + message.Payload);
                }
            }
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
                }
            }
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
                messageHandlers[messageType] = messageHandler;
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
        /// Starts the server.
        /// </summary>
        public void StartAsServer()
        {
            Debug.Log("Starting as server");

            if (Network != null)
            {
                bool success = Network.StartServer(Port, AnnounceMessage);
                if (success)
                {
                    Network.StopListening();
                    IsServer = true;
                    ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
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
            return messageHandlers.Remove(messageType);
        }

        /// <summary>
        /// Handles a new client. All relevant data is sent to the client to get it up to speed.
        /// </summary>
        /// <param name="client">The new client.</param>
        internal void HandleNewClient(Socket client)
        {
            if (!IsServer || Network == null)
            {
                return;
            }

            // Send world anchor data to client
            Services.AnchorManager().SendAnchor(client);

            if (Services.DataManager().CurrentStudyIndex != -1)
            {
                // assign id to client
                var clientMessage = new MessageAcceptClient(clientCounter++);
                Network.SendToClient(clientMessage.Pack(), client);

                // send client information about study
                var studyMessage = new MessageLoadStudy(Services.DataManager().CurrentStudyIndex);
                Network.SendToClient(studyMessage.Pack(), client);

                // send client information about session/condition filters
                var sessionFilterMessage = new MessageUpdateSessionFilter(Services.StudyManager().CurrentStudySessions, Services.StudyManager().CurrentStudyConditions);
                Network.SendToClient(sessionFilterMessage.Pack(), client);

                // send client information about time filter
                var timeFilterMessage = new MessageUpdateTimeFilter(Services.StudyManager().CurrentTimeFilter);
                Network.SendToClient(timeFilterMessage.Pack(), client);

                // send client information about timeline
                var timelineMessage = new MessageUpdateTimeline(new TimelineState(Services.StudyManager().TimelineStatus, Services.StudyManager().CurrentTimestamp, Services.StudyManager().MinTimestamp, Services.StudyManager().MaxTimestamp, Services.StudyManager().PlaybackSpeed));
                Network.SendToClient(timelineMessage.Pack(), client);

                //// send client information about vis containers
                ////foreach (var container in Services.VisManager().ViewContainers.Values)
                ////{
                ////    var visContainer = new VisContainer
                ////    {
                ////        Id = container.Id,
                ////        Orientation = new float[] { container.transform.rotation.x, container.transform.rotation.y, container.transform.rotation.z, container.transform.rotation.w },
                ////        Position = new float[] { container.transform.position.x, container.transform.position.y, container.transform.position.z },
                ////        Scale = new float[] { container.transform.localScale.x, container.transform.localScale.y, container.transform.localScale.z }
                ////    };
                ////    var containerMessage = new MessageCreateVisContainer(visContainer);
                ////    Network.SendToClient(containerMessage.Pack(), client);
                ////}

                // send client information about visualizations
                foreach (var vis in Services.VisManager().Visualizations.Values)
                {
                    var visMessage = new MessageCreateVisualization(vis.Settings);
                    Network.SendToClient(visMessage.Pack(), client);
                }
            }
        }

        /// <summary>
        /// Called when a connection to a server has been established.
        /// </summary>
        internal void OnConnectedToServer()
        {
            IsConnected = true;
            ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Awake()
        {
            messageHandlers = new Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>>();

            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }

        private Task OnBroadcastData(MessageContainer obj)
        {
            Debug.Log("Received broadcast!");
            MessageAnnouncement message = MessageAnnouncement.Unpack(obj); // deserialize message

            // check if the announcement strings matches
            if (message != null && message.Message.Equals(AnnounceMessage))
            {
                if (Sessions.TryGetValue(message.IP, out SessionInfo sessionInfo) == false)
                {
                    // add to session list
                    Sessions.Add(message.IP, new SessionInfo() { SessionName = message.Name, SessionIp = message.IP, SessionPort = message.Port });

                    // trigger event to notify about new session
                    SessionListChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        private void Start()
        {
            // registers callback for announcement handling
            RegisterMessageHandler(MessageContainer.MessageType.ANNOUNCEMENT, OnBroadcastData);

            if (Network != null)
            {
                Network.StartListening();
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