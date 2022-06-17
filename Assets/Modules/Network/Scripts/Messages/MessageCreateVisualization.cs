// ------------------------------------------------------------------------------------
// <copyright file="MessageCreateVisualization.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Text;
using IMLD.MixedRealityAnalysis.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to create a visualization.
    /// </summary>
    public class MessageCreateVisualization : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.CREATE_VISUALIZATION;

        public VisProperties Settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCreateVisualization"/> class.
        /// </summary>
        /// <param name="settings">The settings of the new visualization.</param>
        public MessageCreateVisualization(VisProperties settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageCreateVisualization"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageCreateVisualization Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
#if UNITY_STANDALONE || UNITY_EDITOR
                ,SerializationBinder = new NetworkManager.CustomSerializationBinder()
#endif
            };

            string payload = Encoding.UTF8.GetString(container.Payload);
            var result = JsonConvert.DeserializeObject<MessageCreateVisualization>(payload, jsonSettings);
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageCreateVisualization"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
#if UNITY_STANDALONE || UNITY_EDITOR
                ,SerializationBinder = new NetworkManager.CustomSerializationBinder()
#endif
            };

            string payload = JsonConvert.SerializeObject(this, jsonSettings);
            return new MessageContainer(Type, payload);
        }
    }
}