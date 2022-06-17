// ------------------------------------------------------------------------------------
// <copyright file="MessageLoadStudy.cs" company="Technische Universität Dresden">
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
    /// A network message to load a study.
    /// </summary>
    public class MessageLoadStudy : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.LOAD_STUDY;

        public int StudyIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageLoadStudy"/> class.
        /// </summary>
        /// <param name="studyIndex">The index of the study that should be loaded.</param>
        public MessageLoadStudy(int studyIndex)
        {
            StudyIndex = studyIndex;
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageLoadStudy"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageLoadStudy Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageLoadStudy>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageLoadStudy"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}