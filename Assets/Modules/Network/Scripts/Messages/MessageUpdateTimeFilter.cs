// ------------------------------------------------------------------------------------
// <copyright file="MessageUpdateTimeFilter.cs" company="Technische Universität Dresden">
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
    /// A network message to update the time filter.
    /// </summary>
    public class MessageUpdateTimeFilter : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_TIME_FILTER;

        public TimeFilter TimeFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageUpdateTimeFilter"/> class.
        /// </summary>
        /// <param name="filter">The new <see cref="TimeFilter"/>.</param>
        public MessageUpdateTimeFilter(TimeFilter filter)
        {
            TimeFilter = filter;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageUpdateTimeFilter"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageUpdateTimeFilter Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageUpdateTimeFilter>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageUpdateTimeFilter"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}