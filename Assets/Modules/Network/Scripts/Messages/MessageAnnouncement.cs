// ------------------------------------------------------------------------------------
// <copyright file="MessageAnnouncement.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Text;
using Newtonsoft.Json;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to announce the server.
    /// </summary>
    public class MessageAnnouncement : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ANNOUNCEMENT;
        public string IP;
        public string Message;
        public string Name;
        public int Port;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageAnnouncement"/> class.
        /// </summary>
        /// <param name="broadcastmessage">The message to broadcast.</param>
        /// <param name="ip">The ip that the server accepts connections on.</param>
        /// <param name="name">The name of the server.</param>
        /// <param name="port">The port that the server accepts connections on.</param>
        public MessageAnnouncement(string broadcastmessage, string ip, string name, int port)
        {
            IP = ip;
            Port = port;
            Name = name;
            Message = broadcastmessage;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageAnnouncement"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageAnnouncement Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageAnnouncement>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageAnnouncement"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}