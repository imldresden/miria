// ------------------------------------------------------------------------------------
// <copyright file="MessageUpdateSessionFilter.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IMLD.MixedRealityAnalysis.Network
{
    /// <summary>
    /// A network message to update the session filter.
    /// </summary>
    public class MessageUpdateSessionFilter : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_SESSION_FILTER;

        public List<int> Conditions;
        public List<int> Sessions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageUpdateSessionFilter"/> class.
        /// </summary>
        /// <param name="sessions">The list of session ids to filter for.</param>
        /// <param name="conditions">The list of condition ids to filter for.</param>
        public MessageUpdateSessionFilter(List<int> sessions = null, List<int> conditions = null)
        {
            if (sessions != null)
            {
                Sessions = sessions;
            }
            else
            {
                Sessions = new List<int>();
            }

            if (conditions != null)
            {
                Conditions = conditions;
            }
            else
            {
                Conditions = new List<int>();
            }
        }

        /// <summary>
        /// Unpacks a <see cref="MessageContainer"/> of the type <see cref="MessageUpdateSessionFilter"/>.
        /// </summary>
        /// <param name="container">The container of the message.</param>
        /// <returns>The unpacked message.</returns>
        public static MessageUpdateSessionFilter Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<MessageUpdateSessionFilter>(Encoding.UTF8.GetString(container.Payload));
            return result;
        }

        /// <summary>
        /// Packs this <see cref="MessageUpdateSessionFilter"/> into a <see cref="MessageContainer"/>.
        /// </summary>
        /// <returns>The packed message.</returns>
        public MessageContainer Pack()
        {
            string payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, payload);
        }
    }
}