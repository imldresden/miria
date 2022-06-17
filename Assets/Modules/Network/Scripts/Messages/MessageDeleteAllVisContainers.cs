// ------------------------------------------------------------------------------------
// <copyright file="MessageDeleteAllVisContainers.cs" company="Technische Universität Dresden">
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
    /// A network message to delete all vis/view containers.
    /// </summary>
    public class MessageDeleteAllVisContainers : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.DELETE_ALL_CONTAINERS;

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageDeleteAllVisContainers"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageDeleteAllVisContainers Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageDeleteAllVisContainers>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageDeleteAllVisContainers"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}