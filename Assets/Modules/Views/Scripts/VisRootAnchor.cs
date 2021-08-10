// ------------------------------------------------------------------------------------
// <copyright file="VisRootAnchor.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Core;
using IMLD.MixedRealityAnalysis.Network;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is the root anchor for the visualizations of the scene. It is placed by the user at the origin of the original coordinate system.
    /// The position, orientation and scale of this anchor is synchronized over network.
    /// </summary>
    public class VisRootAnchor : MonoBehaviour
    {
        private bool changedFromNetwork = false;
        private Vector3 targetLocalPosition, previousLocalPosition;
        private Quaternion targetLocalRotation, previousLocalRotation;
        private Vector3 targetLocalScale, previousLocalScale;

        private Task OnRemoteUpdate(MessageContainer obj)
        {
            var message = MessageUpdateOrigin.Unpack(obj);

            targetLocalPosition = message.Position;
            targetLocalRotation = message.Orientation;
            targetLocalScale = new Vector3(message.Scale, message.Scale, message.Scale);
            changedFromNetwork = true;

            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        private void Start()
        {
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_ORIGIN, OnRemoteUpdate);

            previousLocalPosition = transform.localPosition;
            previousLocalRotation = transform.localRotation;
            previousLocalScale = transform.localScale;
        }

        // Update is called once per frame
        private void Update()
        {
            if (changedFromNetwork)
            {
                transform.localPosition = targetLocalPosition;
                transform.localRotation = targetLocalRotation;
                transform.localScale = targetLocalScale;
                changedFromNetwork = false;
            }
            else if (previousLocalPosition != transform.localPosition || previousLocalRotation != transform.localRotation || previousLocalScale != transform.localScale)
            {
                var message = new MessageUpdateOrigin(transform.localPosition, transform.localRotation, transform.localScale.x).Pack();
                Services.NetworkManager().SendMessage(message);
            }

            previousLocalPosition = transform.localPosition;
            previousLocalRotation = transform.localRotation;
            previousLocalScale = transform.localScale;
        }
    }
}