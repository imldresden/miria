// ------------------------------------------------------------------------------------
// <copyright file="MessageCreateVisContainer.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Text;
using IMLD.MixedRealityAnalysis.Core;
using Newtonsoft.Json;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to create a new vis container.
    /// </summary>
    public class MessageCreateVisContainer : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.CREATE_CONTAINER;

        public VisContainer Container;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCreateVisContainer"/> class.
        /// </summary>
        /// <param name="container">The <see cref="VisContainer"/> with the settings.</param>
        public MessageCreateVisContainer(VisContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageCreateVisContainer"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageCreateVisContainer Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageCreateVisContainer>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageCreateVisContainer"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}