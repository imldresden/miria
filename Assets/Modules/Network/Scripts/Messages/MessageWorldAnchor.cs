// ------------------------------------------------------------------------------------
// <copyright file="MessageWorldAnchor.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to transmit a world anchor.
    /// </summary>
    /// <remarks>The payload of this message is binary.</remarks>
    public class MessageWorldAnchor : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.WORLD_ANCHOR;

        public byte[] AnchorData;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageWorldAnchor"/> class.
        /// </summary>
        /// <param name="anchorData">The binary anchor data.</param>
        public MessageWorldAnchor(byte[] anchorData)
        {
            AnchorData = anchorData;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageWorldAnchor"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageWorldAnchor Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            return new MessageWorldAnchor(container.Payload);
        }

        /// <summary>
        /// Packs this <see cref="MessageWorldAnchor"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            return new MessageContainer(Type, AnchorData);
        }
    }
}