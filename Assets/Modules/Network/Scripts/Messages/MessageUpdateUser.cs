// ------------------------------------------------------------------------------------
// <copyright file="MessageUpdateUser.cs" company="Technische Universität Dresden">
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
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to update a user indicator.
    /// </summary>
    public class MessageUpdateUser : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_USER;

        [JsonProperty]
        public float ColorB;

        [JsonProperty]
        public float ColorG;

        [JsonProperty]
        public float ColorR;

        [JsonProperty]
        public Guid Id;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageUpdateUser"/> class.
        /// </summary>
        /// <param name="position">The new position of the user.</param>
        /// <param name="orientation">The new orientation of the user.</param>
        /// <param name="id">The <see cref="Guid"/> of the user.</param>
        /// <param name="color">The color of the user indicator.</param>
        public MessageUpdateUser(Vector3 position, Quaternion orientation, Guid id, Color color)
        {
            PosX = position.x;
            PosY = position.y;
            PosZ = position.z;
            RotX = orientation.x;
            RotY = orientation.y;
            RotZ = orientation.z;
            RotW = orientation.w;
            ColorR = color.r;
            ColorG = color.g;
            ColorB = color.b;
            Id = id;
        }

        ///// <summary>
        ///// Gets or sets the color of the user update.
        ///// </summary>
        //[JsonIgnore]
        //public Guid Id
        //{
        //    get
        //    {
        //        return new Guid(IdString);
        //    }

        //    set
        //    {
        //        IdString = value.ToString();
        //    }
        //}

        /// <summary>
        /// Gets or sets the color of the user update.
        /// </summary>
        [JsonIgnore]
        public Color Color
        {
            get
            {
                return new Color(ColorR, ColorG, ColorB);
            }

            set
            {
                ColorR = value.r;
                ColorG = value.g;
                ColorB = value.b;
            }
        }

        /// <summary>
        /// Gets or sets the orientation of the user update.
        /// </summary>
        [JsonIgnore]
        public Quaternion Orientation
        {
            get
            {
                return new Quaternion(RotX, RotY, RotZ, RotW);
            }

            set
            {
                RotX = value.x;
                RotY = value.y;
                RotZ = value.z;
                RotW = value.w;
            }
        }

        /// <summary>
        /// Gets or sets the position of the user update.
        /// </summary>
        [JsonIgnore]
        public Vector3 Position
        {
            get
            {
                return new Vector3(PosX, PosY, PosZ);
            }

            set
            {
                PosX = value.x;
                PosY = value.y;
                PosZ = value.z;
            }
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageUpdateUser"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageUpdateUser Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<MessageUpdateUser>(Encoding.UTF8.GetString(container.Payload));
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Packs this <see cref="MessageUpdateUser"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}