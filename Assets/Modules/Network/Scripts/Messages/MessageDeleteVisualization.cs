// ------------------------------------------------------------------------------------
// <copyright file="MessageDeleteVisualization.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Text;
using Newtonsoft.Json;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to delete a visualization.
    /// </summary>
    public class MessageDeleteVisualization : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.DELETE_VISUALIZATION;

        public Guid VisId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDeleteVisualization"/> class.
        /// </summary>
        /// <param name="visId">The <see cref="Guid"/> of the visualization that should be deleted.</param>
        public MessageDeleteVisualization(Guid visId)
        {
            VisId = visId;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageDeleteVisualization"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageDeleteVisualization Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageDeleteVisualization>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageDeleteVisualization"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}