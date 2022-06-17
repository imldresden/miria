// ------------------------------------------------------------------------------------
// <copyright file="MessageUpdateAnnotation.cs" company="Technische Universität Dresden">
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
    /// A network message to update an annotation.
    /// </summary>
    public class MessageUpdateAnnotation : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_ANNOTATION;

        [JsonProperty]
        public float ColorB;

        [JsonProperty]
        public float ColorG;

        [JsonProperty]
        public float ColorR;

        public Guid Id;

        [JsonProperty]
        public float PosX;

        [JsonProperty]
        public float PosY;

        [JsonProperty]
        public float PosZ;

        public string Text; // ToDo: Support text annotations.

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageUpdateAnnotation"/> class.
        /// </summary>
        /// <param name="id">The <see cref="Guid"/> of the annotation.</param>
        /// <param name="position">The position of the annotation.</param>
        /// <param name="color">The color of the annotation.</param>
        /// <param name="text">An optional text for the annotation. Not supported yet!</param>
        public MessageUpdateAnnotation(Guid id, Vector3 position, Color color, string text = "")
        {
            PosX = position.x;
            PosY = position.y;
            PosZ = position.z;
            ColorR = color.r;
            ColorG = color.g;
            ColorB = color.b;
            Text = text;
            Id = id;
        }

        /// <summary>
        /// Gets the color of the annotation.
        /// </summary>
        [JsonIgnore]
        public Color Color
        {
            get { return new Color(ColorR, ColorG, ColorB); }
        }

        /// <summary>
        /// Gets the position of the annotation.
        /// </summary>
        [JsonIgnore]
        public Vector3 Position
        {
            get { return new Vector3(PosX, PosY, PosZ); }
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageUpdateAnnotation"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageUpdateAnnotation Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<MessageUpdateAnnotation>(Encoding.UTF8.GetString(container.Payload));
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Packs this <see cref="MessageUpdateAnnotation"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}