// ------------------------------------------------------------------------------------
// <copyright file="MessageDeleteAllVisualizations.cs" company="Technische Universität Dresden">
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
    /// A network message to delete all visualizations.
    /// </summary>
    public class MessageDeleteAllVisualizations : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.DELETE_ALL_VISUALIZATIONS;

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageDeleteAllVisualizations"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageDeleteAllVisualizations Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageDeleteAllVisualizations>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageDeleteAllVisualizations"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}