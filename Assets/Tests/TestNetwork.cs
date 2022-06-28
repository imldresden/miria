using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Core;
using IMLD.MixedRealityAnalysis.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    [TestFixture]
    public class TestNetwork
    {
        string announceMessage = "MIRIA";
        int port = 11337;
        GameObject networkGameObjectServer;
        GameObject networkGameObjectClient;
        GameObject networkManagerGameObject;
        float time;
        bool clientAccepted;
        bool userUpdated;
        bool worldAnchorReceived;
        MessageContainer containerUpdateUser;
        MessageContainer containerWorldAnchor;

        [SetUp]
        public void Setup()
        {
            Debug.Log("SetUp");

            containerUpdateUser = null;
            containerWorldAnchor = null;
            time = 0.0f;
            clientAccepted = false;
            userUpdated = false;
            worldAnchorReceived = false;

            networkGameObjectServer = new GameObject();
            networkGameObjectServer.AddComponent<NetworkTransport>();

            networkGameObjectClient = new GameObject();
            networkGameObjectClient.AddComponent<NetworkTransport>();

            networkManagerGameObject = new GameObject();
            networkManagerGameObject.AddComponent<NetworkManager>();
            NetworkManager.Instance.RegisterMessageHandler(MessageContainer.MessageType.ACCEPT_CLIENT, OnAcceptClient);
            NetworkManager.Instance.RegisterMessageHandler(MessageContainer.MessageType.UPDATE_USER, OnUpdateUser);
            NetworkManager.Instance.RegisterMessageHandler(MessageContainer.MessageType.WORLD_ANCHOR, OnWorldAnchor);
        }

        [TearDown]
        public void Dispose()
        {
            Debug.Log("TearDown");
            GameObject.DestroyImmediate(networkGameObjectServer);
            GameObject.DestroyImmediate(networkGameObjectClient);
            GameObject.DestroyImmediate(networkManagerGameObject);
            containerUpdateUser = null;
            containerWorldAnchor = null;
            time = 0.0f;
            clientAccepted = false;
            userUpdated = false;
            worldAnchorReceived = false;
        }


        private Task OnAcceptClient(MessageContainer message)
        {
            clientAccepted = true;
            return Task.CompletedTask;
        }

        private Task OnUpdateUser(MessageContainer message)
        {
            userUpdated = true;
            containerUpdateUser = message;
            return Task.CompletedTask;
        }

        private Task OnWorldAnchor(MessageContainer message)
        {
            worldAnchorReceived = true;
            containerWorldAnchor = message;
            return Task.CompletedTask;
        }

        [Test]
        public void TestMessageAcceptClient()
        {
            const int id = 3;
            MessageAcceptClient messageAcceptClient = new MessageAcceptClient(id);
            MessageContainer container = messageAcceptClient.Pack();
            MessageAcceptClient unpackedMessageAcceptClient = MessageAcceptClient.Unpack(container);
            Assert.IsNotNull(unpackedMessageAcceptClient, "Failed to pack and unpack message of type MessageAcceptClient.");
            Assert.IsTrue(unpackedMessageAcceptClient.ClientIndex == id, "Failed to pack and unpack message of type MessageAcceptClient.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageAcceptClient.");
        }

        [Test]
        public void TestMessageAnnouncement()
        {
            const string message = "hello";
            const string ip = "192.168.0.1";
            const string name = "testserver";
            const int port = 8080;
            MessageAnnouncement messageAnnouncement = new MessageAnnouncement(message, ip, name, port);
            MessageContainer container = messageAnnouncement.Pack();
            MessageAnnouncement unpackedMessageAnnouncement = MessageAnnouncement.Unpack(container);
            Assert.IsNotNull(unpackedMessageAnnouncement, "Failed to pack and unpack message of type MessageAnnouncement.");
            Assert.IsTrue(unpackedMessageAnnouncement.Message == message
                && unpackedMessageAnnouncement.IP == ip
                && unpackedMessageAnnouncement.Name == name
                && unpackedMessageAnnouncement.Port == port,
                "Failed to pack and unpack message of type MessageAnnouncement.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageAnnouncement.");
        }

        [Test]
        public void TestMessageCenterData()
        {
            const bool value = true;
            var message = new MessageCenterData(value);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageCenterData.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageCenterData.");
            Assert.IsTrue(unpackedMessage.IsCentering==value, "Failed to pack and unpack message of type MessageCenterData.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageCenterData.");
        }

        [Test]
        public void TestMessageCreateVisContainer()
        {
            VisContainer visContainer = new VisContainer
            {
                Position = new float[] { 3, 4, 5 },
                Scale = new float[] { 1, 1, 1 },
                Orientation = new float[] { 2, 3, 4, 1 },
                Id = 42,
                ParentId = 9,
            };
            var message = new MessageCreateVisContainer(visContainer);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageCreateVisContainer.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageCreateVisContainer.");
            Assert.IsTrue(unpackedMessage.Container.Id == visContainer.Id
                && unpackedMessage.Container.Orientation[3] == visContainer.Orientation[3]
                && unpackedMessage.Container.ParentId == visContainer.ParentId
                && unpackedMessage.Container.Position[2] == visContainer.Position[2]
                && unpackedMessage.Container.Scale[2] == visContainer.Scale[2],
                "Failed to pack and unpack message of type MessageCreateVisContainer.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageCreateVisContainer.");
        }

        [Test]
        public void TestMessageCreateVisualization()
        {
            VisProperties visProperties = new VisProperties(new Guid(), VisType.Media2D, 0);
            var message = new MessageCreateVisualization(visProperties);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageCreateVisualization.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageCreateVisualization.");
            Assert.IsTrue(unpackedMessage.Settings.AnchorId == visProperties.AnchorId
                && unpackedMessage.Settings.VisId == visProperties.VisId
                && unpackedMessage.Settings.VisType == visProperties.VisType,
                "Failed to pack and unpack message of type MessageCreateVisualization.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageCreateVisualization.");
        }

        [Test]
        public void TestMessageDeleteAllVisContainers()
        {
            var message = new MessageDeleteAllVisContainers();
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageDeleteAllVisContainers.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageDeleteAllVisContainers.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageDeleteAllVisContainers.");
        }

        [Test]
        public void TestMessageDeleteAllVisualizations()
        {
            var message = new MessageDeleteAllVisualizations();
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageDeleteAllVisualizations.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageDeleteAllVisualizations.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageDeleteAllVisualizations.");
        }

        [Test]
        public void TestMessageDeleteVisualization()
        {
            Guid value = new Guid();
            var message = new MessageDeleteVisualization(value);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageDeleteVisualization.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageDeleteVisualization.");
            Assert.IsTrue(unpackedMessage.VisId == value, "Failed to pack and unpack message of type MessageDeleteVisualization.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageDeleteVisualization.");
        }

        [Test]
        public void TestMessageLoadStudy()
        {
            const int value = 2;
            var message = new MessageLoadStudy(value);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageLoadStudy.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageLoadStudy.");
            Assert.IsTrue(unpackedMessage.StudyIndex == value, "Failed to pack and unpack message of type MessageLoadStudy.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageLoadStudy.");
        }

        [Test]
        public void TestMessageUpdateAnnotation()
        {
            Guid guid = new Guid();
            Vector3 position = new Vector3(2, 3, 4);
            Color color = Color.cyan;
            string annotation = "test";
            var message = new MessageUpdateAnnotation(guid, position, color, annotation);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageUpdateAnnotation.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageUpdateAnnotation.");
            Assert.IsTrue(unpackedMessage.Color == color
                && unpackedMessage.Id == guid
                && unpackedMessage.Position == position
                && unpackedMessage.Text == annotation
                , "Failed to pack and unpack message of type MessageUpdateAnnotation.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateAnnotation.");
        }

        [Test]
        public void TestMessageUpdateOrigin()
        {
            Vector3 position = new Vector3(1, 2, 3);
            Quaternion orientation = Quaternion.Euler(30, 60, 45);
            float scale = 2.0f;
            var message = new MessageUpdateOrigin(position, orientation, scale);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageUpdateOrigin.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageUpdateOrigin.");
            Assert.IsTrue(unpackedMessage.Orientation == orientation
                && unpackedMessage.Position == position
                && unpackedMessage.Scale == scale
                , "Failed to pack and unpack message of type MessageUpdateOrigin.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateOrigin.");
        }

        [Test]
        public void TestMessageUpdateSessionFilter()
        {
            List<int> sessions = new List<int>();
            sessions.Add(42);
            List<int> conditions = new List<int>();
            conditions.Add(815);
            var message = new MessageUpdateSessionFilter(sessions, conditions);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageUpdateSessionFilter.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageUpdateSessionFilter.");
            Assert.IsTrue(unpackedMessage.Conditions[0] == conditions[0]
                && unpackedMessage.Sessions[0] == sessions[0]
                , "Failed to pack and unpack message of type MessageUpdateSessionFilter.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateSessionFilter.");
        }

        [Test]
        public void TestMessageUpdateTimeFilter()
        {
            TimeFilter filter = new TimeFilter();
            filter.MaxTime = 0.75f;
            filter.MinTime = 0.1f;
            filter.MaxTimestamp = 101;
            filter.MinTimestamp = 22;
            var message = new MessageUpdateTimeFilter(filter);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageUpdateTimeFilter.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageUpdateTimeFilter.");
            Assert.IsTrue(unpackedMessage.TimeFilter.MaxTime == filter.MaxTime
                && unpackedMessage.TimeFilter.MinTime == filter.MinTime
                && unpackedMessage.TimeFilter.MaxTimestamp == filter.MaxTimestamp
                && unpackedMessage.TimeFilter.MinTimestamp == filter.MinTimestamp
                , "Failed to pack and unpack message of type MessageUpdateTimeFilter.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateTimeFilter.");
        }

        [Test]
        public void TestMessageUpdateTimeline()
        {
            TimelineState status = new TimelineState();
            status.CurrentTimestamp = 1234;
            status.MaxTimestamp = 3000;
            status.MinTimestamp = 1000;
            status.PlaybackSpeed = 2f;
            status.TimelineStatus = TimelineStatus.PAUSED;
            var message = new MessageUpdateTimeline(status);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageUpdateTimeline.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageUpdateTimeline.");
            Assert.IsTrue(unpackedMessage.TimelineState.CurrentTimestamp == status.CurrentTimestamp
                && unpackedMessage.TimelineState.MaxTimestamp == status.MaxTimestamp
                && unpackedMessage.TimelineState.MinTimestamp == status.MinTimestamp
                && unpackedMessage.TimelineState.PlaybackSpeed == status.PlaybackSpeed
                && unpackedMessage.TimelineState.TimelineStatus == status.TimelineStatus
                , "Failed to pack and unpack message of type MessageUpdateTimeline.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateTimeline.");
        }

        [Test]
        public void TestMessageUpdateUser()
        {
            MessageUpdateUser messageUpdateUser = new MessageUpdateUser(new Vector3(1.0f, 2.0f, -3.4f), new Quaternion(-1.0f, 2.0f, 3.1f, 1.0f), Guid.NewGuid(), Color.black);
            MessageContainer container = messageUpdateUser.Pack();
            MessageUpdateUser unpackedMessageUpdateUser = MessageUpdateUser.Unpack(container);
            Assert.IsNotNull(unpackedMessageUpdateUser, "Failed to pack and unpack message of type MessageUpdateUser.");
            Assert.IsTrue(unpackedMessageUpdateUser.Position == messageUpdateUser.Position
                && unpackedMessageUpdateUser.Orientation == messageUpdateUser.Orientation
                && unpackedMessageUpdateUser.Id == messageUpdateUser.Id
                && unpackedMessageUpdateUser.Color == messageUpdateUser.Color,
                "Failed to pack and unpack message of type MessageUpdateUser.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateUser.");
        }

        [Test]
        public void TestMessageUpdateVisContainer()
        {
            VisContainer visContainer = new VisContainer
            {
                Position = new float[] { 3, 4, 5 },
                Scale = new float[] { 1, 1, 1 },
                Orientation = new float[] { 2, 3, 4, 1 },
                Id = 42,
                ParentId = 9,
            };
            var message = new MessageUpdateVisContainer(visContainer);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageUpdateVisContainer.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageUpdateVisContainer.");
            Assert.IsTrue(unpackedMessage.Container.Id == visContainer.Id
                && unpackedMessage.Container.Orientation[3] == visContainer.Orientation[3]
                && unpackedMessage.Container.ParentId == visContainer.ParentId
                && unpackedMessage.Container.Position[2] == visContainer.Position[2]
                && unpackedMessage.Container.Scale[2] == visContainer.Scale[2],
                "Failed to pack and unpack message of type MessageUpdateVisContainer.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateVisContainer.");
        }

        [Test]
        public void TestMessageUpdateVisualization()
        {
            VisProperties visProperties = new VisProperties(new Guid(), VisType.Media2D, 0);
            var message = new MessageUpdateVisualization(visProperties);
            MessageContainer container = message.Pack();
            var unpackedMessage = MessageUpdateVisualization.Unpack(container);
            Assert.IsNotNull(unpackedMessage, "Failed to pack and unpack message of type MessageUpdateVisualization.");
            Assert.IsTrue(unpackedMessage.Settings.AnchorId == visProperties.AnchorId
                && unpackedMessage.Settings.VisId == visProperties.VisId
                && unpackedMessage.Settings.VisType == visProperties.VisType,
                "Failed to pack and unpack message of type MessageUpdateVisualization.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageUpdateVisualization.");
        }

        [Test]
        public void TestMessageWorldAnchor()
        {
            byte[] data = new byte[] { 255, 255, 0, 0 };
            MessageWorldAnchor messageWorldAnchor = new MessageWorldAnchor(data);
            MessageContainer container = messageWorldAnchor.Pack();
            MessageWorldAnchor unpackedMessageWorldAnchor = MessageWorldAnchor.Unpack(container);
            Assert.IsNotNull(messageWorldAnchor, "Failed to pack and unpack message of type MessageWorldAnchor.");
            Assert.IsTrue(unpackedMessageWorldAnchor.AnchorData.SequenceEqual(messageWorldAnchor.AnchorData), "Failed to pack and unpack message of type MessageWorldAnchor.");
            Debug.Log("TEST OK: Successfully packed and unpacked message of type MessageWorldAnchor.");
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // 'yield return null;' to skip a frame.
        [UnityTest]
        public IEnumerator TestNetworkConnectToServer()
        {
            NetworkTransport server = networkGameObjectServer.GetComponent<NetworkTransport>();
            NetworkTransport client = networkGameObjectClient.GetComponent<NetworkTransport>();
            NetworkManager.Instance.Network = server;
            NetworkManager.Instance.Port = port;
            NetworkManager.Instance.AnnounceMessage = announceMessage;

            Assert.IsTrue(NetworkManager.Instance.StartAsServer(), "Failed to start the server.");
            Debug.Log("TEST OK: Server started.");

            Assert.IsTrue(NetworkManager.Instance.IsServer, "Failed to report correct server status.");
            Debug.Log("TEST OK: NetworkManager configured as server.");

            yield return null;

            server.Pause();
            Assert.IsTrue(server.IsPaused == true, "Failed to pause server.");
            Debug.Log("TEST OK: Paused server.");

            server.Unpause();
            Assert.IsTrue(server.IsPaused == false, "Failed to unpause server.");
            Debug.Log("TEST OK: Unpaused server.");

            Assert.IsTrue(client.StartListening(), "Failed to start listening for announcements.");
            Debug.Log("TEST OK: Started listening for announcements.");

            string ip = server.ServerIPs[0];
            Assert.IsTrue(client.ConnectToServer(ip, port), "Failed to start connecting to the server.");
            Debug.Log("TEST OK: Started connecting to server.");
            time = 0.0f;

            while (!client.IsConnected && time < 4.0f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(client.IsConnected, "Failed to connect to the server.");
            Debug.Log("TEST OK: Connected to server.");

            time = 0.0f;

            while (!clientAccepted && time < 4.0f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(clientAccepted, "Client not accepted.");
            Debug.Log("TEST OK: Client accepted by server.");

            MessageUpdateUser messageUpdateUser = new MessageUpdateUser(Vector3.zero, Quaternion.identity, Guid.NewGuid(), Color.white);
            client.SendToServer(messageUpdateUser.Pack());

            time = 0.0f;

            while (!userUpdated && time < 4.0f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(userUpdated, "Sending JSON message to server failed.");
            Debug.Log("TEST OK: JSON message successfully sent to server.");

            Assert.IsNotNull(containerUpdateUser, "Receiving JSON message failed.");
            MessageUpdateUser messageReceivedUpdateUser = MessageUpdateUser.Unpack(containerUpdateUser);
            Assert.IsTrue(messageReceivedUpdateUser.Position == messageUpdateUser.Position
                && messageReceivedUpdateUser.Orientation == messageUpdateUser.Orientation
                && messageReceivedUpdateUser.Id == messageUpdateUser.Id
                && messageReceivedUpdateUser.Color == messageUpdateUser.Color, "Receiving JSON message failed.");
            Debug.Log("TEST OK: JSON message successfully sent to server.");

            byte[] worldAnchorData = new byte[66000]; // large message that will be fractured
            var rnd = new System.Random();
            rnd.NextBytes(worldAnchorData);

            MessageWorldAnchor messageWorldAnchor = new MessageWorldAnchor(worldAnchorData);
            client.SendToServer(messageWorldAnchor.Pack());

            time = 0.0f;

            while (!worldAnchorReceived && time < 4.0f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(worldAnchorReceived, "Sending binary message to server failed.");
            Debug.Log("TEST OK: binary message successfully sent to server.");

            Assert.IsNotNull(containerWorldAnchor, "Receiving binary message failed.");
            MessageWorldAnchor messageReceivedWorldAnchor = MessageWorldAnchor.Unpack(containerWorldAnchor);
            Assert.IsTrue(messageReceivedWorldAnchor.AnchorData.SequenceEqual(messageWorldAnchor.AnchorData), "Receiving binary message failed.");
            Debug.Log("TEST OK: binary message successfully received.");

            networkGameObjectServer.GetComponent<NetworkTransport>().StopServer();
        }
    }
}
