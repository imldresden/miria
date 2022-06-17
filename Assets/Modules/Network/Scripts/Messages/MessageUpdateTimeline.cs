// ------------------------------------------------------------------------------------
// <copyright file="MessageUpdateTimeline.cs" company="Technische Universität Dresden">
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
    /// A network message to update the timeline.
    /// </summary>
    public class MessageUpdateTimeline : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_TIMELINE;

        public TimelineState TimelineState;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageUpdateTimeline"/> class.
        /// </summary>
        /// <param name="status">The new <see cref="TimelineState"/>.</param>
        public MessageUpdateTimeline(TimelineState status)
        {
            TimelineState = status;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageUpdateTimeline"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageUpdateTimeline Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageUpdateTimeline>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageUpdateTimeline"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}