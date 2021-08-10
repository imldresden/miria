// ------------------------------------------------------------------------------------
// <copyright file="MessageContainer.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Text;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A container for all network messages.
    /// </summary>
    public class MessageContainer
    {
        public const byte FirstJsonMessageType = 128;
        public byte[] Payload;
        public IPEndPoint Sender;

        public MessageType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContainer"/> class.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/> of the payload.</param>
        /// <param name="payload">The payload string.</param>
        public MessageContainer(MessageType type, string payload)
        {
            Type = type;
            Payload = Encoding.UTF8.GetBytes(payload);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContainer"/> class.
        /// </summary>
        /// <param name="type">The <see cref="MessageType"/> of the payload.</param>
        /// <param name="payload">The payload byte array.</param>
        public MessageContainer(MessageType type, byte[] payload)
        {
            Type = type;
            Payload = payload;
        }

        /// <summary>
        /// An enum that represents message types.
        /// </summary>
        public enum MessageType
        {
            /// <summary>
            /// Message payload is binary and contains world anchor data.
            /// </summary>
            WORLD_ANCHOR,

            /// <summary>
            /// Message payload is a string and contains a server announcement.
            /// </summary>
            ANNOUNCEMENT = FirstJsonMessageType,

            /// <summary>
            /// Message payload is a string and contains an origin update.
            /// </summary>
            UPDATE_ORIGIN,

            /// <summary>
            /// Message payload is a string and contains a visualization update.
            /// </summary>
            UPDATE_VISUALIZATION,

            /// <summary>
            /// Message payload is a string and contains a timeline update.
            /// </summary>
            UPDATE_TIMELINE,

            /// <summary>
            /// Message payload is a string and contains a time filter update.
            /// </summary>
            UPDATE_TIME_FILTER,

            /// <summary>
            /// Message payload is a string and contains a session filter update.
            /// </summary>
            UPDATE_SESSION_FILTER,

            /// <summary>
            /// Message payload is a string and contains a user update.
            /// </summary>
            UPDATE_USER,

            /// <summary>
            /// Message payload is a string and contains the command to load a study.
            /// </summary>
            LOAD_STUDY,

            /// <summary>
            /// Message payload is a string and contains the command to create a visualization.
            /// </summary>
            CREATE_VISUALIZATION,

            /// <summary>
            /// Message payload is a string and contains the command to create a view container.
            /// </summary>
            CREATE_CONTAINER,

            /// <summary>
            /// Message payload is a string and contains the command to update a view container.
            /// </summary>
            UPDATE_CONTAINER,

            /// <summary>
            /// Message payload is a string and contains the command to delete a visualization.
            /// </summary>
            DELETE_VISUALIZATION,

            /// <summary>
            /// Message payload is a string and contains the command to delete all visualizations.
            /// </summary>
            DELETE_ALL_VISUALIZATIONS,

            /// <summary>
            /// Message payload is a string and contains the command to delete all containers.
            /// </summary>
            DELETE_ALL_CONTAINERS,

            /// <summary>
            /// Message payload is a string and contains an annotation update.
            /// </summary>
            UPDATE_ANNOTATION,

            /// <summary>
            /// Message payload is a string and contains the acceptance of a newly connected client.
            /// </summary>
            ACCEPT_CLIENT,

            /// <summary>
            /// Message payload is a bool and contains the command to center or un-center the data.
            /// </summary>
            CENTER_DATA
        }

        /// <summary>
        /// Deserializes raw data of a known message type to a <see cref="MessageContainer"/>.
        /// </summary>
        /// <param name="sender">The <see cref="IPEndPoint"/> of the sender.</param>
        /// <param name="payload">The raw data.</param>
        /// <param name="messageType">The message type as a <see cref="byte"/>.
        /// Has to be one of the supported <see cref="MessageType">MessageTypes</see></param>
        /// <returns>The deserialized <see cref="MessageContainer"/>.</returns>
        public static MessageContainer Deserialize(IPEndPoint sender, byte[] payload, byte messageType)
        {
            MessageType type = (MessageType)messageType;
            var message = new MessageContainer(type, payload)
            {
                Sender = sender
            };
            return message;
        }

        /// <summary>
        /// Deserializes raw data to a <see cref="MessageContainer"/>.
        /// </summary>
        /// <param name="sender">The <see cref="IPEndPoint"/> of the sender.</param>
        /// <param name="data">The raw data.</param>
        /// <returns>The deserialized <see cref="MessageContainer"/>.</returns>
        public static MessageContainer Deserialize(IPEndPoint sender, byte[] data)
        {
            byte type = data[4];
            byte[] payload = new byte[data.Length - 5];
            Array.Copy(data, 5, payload, 0, data.Length - 5);
            return Deserialize(sender, payload, type);
        }

        /// <summary>
        /// Serializes the message container to a byte array.
        /// </summary>
        /// <returns>The byte of the serialized <see cref="MessageContainer"/>.</returns>
        public byte[] Serialize()
        {
            byte[] envelope = new byte[Payload.Length + 5];
            Array.Copy(BitConverter.GetBytes(Payload.Length), envelope, 4);
            envelope[4] = (byte)Type;
            Array.Copy(Payload, 0, envelope, 5, Payload.Length);
            return envelope;
        }
    }
}