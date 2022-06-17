// ------------------------------------------------------------------------------------
// <copyright file="MessageUpdateOrigin.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to update the scene origin.
    /// </summary>
    public class MessageUpdateOrigin : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_ORIGIN;

        [JsonProperty]
        public float PosX;

        [JsonProperty]
        public float PosY;

        [JsonProperty]
        public float PosZ;

        [JsonProperty]
        public float RotW;

        [JsonProperty]
        public float RotX;

        [JsonProperty]
        public float RotY;

        [JsonProperty]
        public float RotZ;

        public float Scale;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageUpdateOrigin"/> class.
        /// </summary>
        /// <param name="position">The new position of the origin.</param>
        /// <param name="orientation">The new orientation of the origin.</param>
        /// <param name="scale">The new scale of the origin.</param>
        public MessageUpdateOrigin(Vector3 position, Quaternion orientation, float scale)
        {
            PosX = position.x;
            PosY = position.y;
            PosZ = position.z;
            RotX = orientation.x;
            RotY = orientation.y;
            RotZ = orientation.z;
            RotW = orientation.w;
            Scale = scale;
        }

        /// <summary>
        /// Gets the orientation of the origin.
        /// </summary>
        [JsonIgnore]
        public Quaternion Orientation
        {
            get { return new Quaternion(RotX, RotY, RotZ, RotW); }
        }

        /// <summary>
        /// Gets the position of the origin.
        /// </summary>
        [JsonIgnore]
        public Vector3 Position
        {
            get { return new Vector3(PosX, PosY, PosZ); }
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageUpdateOrigin"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageUpdateOrigin Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageUpdateOrigin>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageUpdateOrigin"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}