// ------------------------------------------------------------------------------------
// <copyright file="MessageCenterData.cs" company="Technische Universität Dresden">
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
    /// A network message to center or un-center the data.
    /// </summary>
    public class MessageCenterData : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.CENTER_DATA;

        public bool IsCentering;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCenterData"/> class.
        /// </summary>
        /// <param name="isCentering">Whether the data should be shifted to the origin or not.</param>
        public MessageCenterData(bool isCentering)
        {
            IsCentering = isCentering;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageCenterData"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageCenterData Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageCenterData>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageCenterData"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}