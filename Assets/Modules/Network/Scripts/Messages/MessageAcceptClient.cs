// ------------------------------------------------------------------------------------
// <copyright file="MessageAcceptClient.cs" company="Technische Universität Dresden">
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
    /// A network message to accept a new client connection.
    /// </summary>
    public class MessageAcceptClient : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ACCEPT_CLIENT;

        public int ClientIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageAcceptClient"/> class.
        /// </summary>
        /// <param name="clientIndex">The index of the client.</param>
        public MessageAcceptClient(int clientIndex)
        {
            ClientIndex = clientIndex;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageAcceptClient"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageAcceptClient Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageAcceptClient>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageAcceptClient"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}