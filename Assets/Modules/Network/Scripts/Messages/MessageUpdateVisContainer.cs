// ------------------------------------------------------------------------------------
// <copyright file="MessageUpdateVisContainer.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Text;
using Newtonsoft.Json;
using IMLD.MixedRealityAnalysis.Core;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to update a <see cref="IMLD.MixedRealityAnalysis.Views.VisContainer"/> transform.
    /// </summary>
    public class MessageUpdateVisContainer : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_CONTAINER;

        public VisContainer Container;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageUpdateVisContainer"/> class.
        /// </summary>
        /// <param name="container">The updated <see cref="IMLD.MixedRealityAnalysis.Views.VisContainer"/>.</param>
        public MessageUpdateVisContainer(VisContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageUpdateVisContainer"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageUpdateVisContainer Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageUpdateVisContainer>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageUpdateVisContainer"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}