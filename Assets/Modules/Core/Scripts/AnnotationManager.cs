// ------------------------------------------------------------------------------------
// <copyright file="AnnotationManager.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Network;
using IMLD.MixedRealityAnalysis.Views;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This component is used to manage the creation, update, and deletion of <see cref="AnnotationTag"/>s in the scene.
    /// </summary>
    public class AnnotationManager : MonoBehaviour
    {
        /// <summary>
        /// The Prefab for the <see cref="AnnotationTag"/>s created by this component.
        /// </summary>
        public AnnotationTag AnnotationPrefab;

        private readonly Dictionary<Guid, AnnotationTag> annotationList = new Dictionary<Guid, AnnotationTag>();
        private Transform worldAnchor;

        /// <summary>
        /// This method adds a new annotation.
        /// </summary>
        public void AddAnnotation()
        {
            if (!AnnotationPrefab)
            {
                return;
            }

            Transform cameraTransform = CameraCache.Main ? CameraCache.Main.transform : null;
            foreach (var source in CoreServices.InputSystem.DetectedInputSources)
            {
                // Only look for hands
                if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
                {
                    foreach (var pointer in source.Pointers)
                    {
                        if (pointer is IMixedRealityNearPointer || true)
                        {
                            Vector3 position;
                            if (pointer.Result != null)
                            {
                                position = pointer.Result.Details.Point;
                            }
                            else
                            {
                                position = pointer.Position;
                            }

                            if (cameraTransform != null)
                            {
                                if ((cameraTransform.position - position).magnitude > 3)
                                {
                                    position = cameraTransform.position + ((position - cameraTransform.position) * 0.5f);
                                }
                            }

                            var annotation = Instantiate(AnnotationPrefab, worldAnchor);
                            annotation.transform.position = position;
                            annotation.Id = Guid.NewGuid();
                            annotation.SetColor(Services.UserManager().Color);
                            annotationList.Add(annotation.Id, annotation);

                            var message = new MessageUpdateAnnotation(annotation.Id, annotation.transform.localPosition, annotation.Color);
                            Services.NetworkManager().SendMessage(message.Pack());

                            return; // only take first valid pointer
                        }
                    }
                }
            }

            if (cameraTransform != null)
            {
                Vector3 position = cameraTransform.position;
                var annotation = Instantiate(AnnotationPrefab, worldAnchor);
                annotation.transform.position = position;
                annotation.Id = Guid.NewGuid();
                annotation.SetColor(Services.UserManager().Color);
                annotationList.Add(annotation.Id, annotation);

                var message = new MessageUpdateAnnotation(annotation.Id, annotation.transform.localPosition, annotation.Color);
                Services.NetworkManager().SendMessage(message.Pack());

                return; // only take first valid pointer
            }
        }

        private Task OnAnnotationUpdated(MessageContainer obj)
        {
            MessageUpdateAnnotation message = MessageUpdateAnnotation.Unpack(obj);

            if (annotationList.ContainsKey(message.Id))
            {
                var annotation = annotationList[message.Id];
                annotation.transform.localPosition = message.Position;
            }
            else if (AnnotationPrefab)
            {
                var annotation = Instantiate(AnnotationPrefab, worldAnchor);
                annotation.transform.localPosition = message.Position;
                annotation.Id = message.Id;
                annotation.SetColor(message.Color);
                annotationList.Add(annotation.Id, annotation);
            }

            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        private void Start()
        {
            if (Services.VisManager() != null)
            {
                worldAnchor = Services.VisManager().VisAnchor;
            }
            else
            {
                worldAnchor = this.transform;
            }

            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_ANNOTATION, OnAnnotationUpdated);
        }
    }
}