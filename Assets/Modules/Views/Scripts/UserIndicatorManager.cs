// ------------------------------------------------------------------------------------
// <copyright file="UserIndicatorManager.cs" company="Technische Universität Dresden">
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
using IMLD.MixedRealityAnalysis.Utils;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This Unity component manages user indicators. These small objects show the status and position of participants in the analysis session.
    /// </summary>
    public class UserIndicatorManager : MonoBehaviour
    {
        public Color Color;
        public Transform LocalUserPosition;
        public SimpleInterpolator UserIndicatorPrefab;
        private Guid id = Guid.NewGuid();

        private readonly Dictionary<Guid, SimpleInterpolator> indicatorList = new Dictionary<Guid, SimpleInterpolator>();
        private MessageUpdateUser message;
        private Transform worldAnchor;
        private Transform cameraTransform;
        

        private Task OnAcceptedAsClient(MessageContainer obj)
        {
            MessageAcceptClient message = MessageAcceptClient.Unpack(obj);
            SetColor(message.ClientIndex);

            // send position update, so that the other users get up to speed
            SendUserUpdate();

            return Task.CompletedTask;
        }

        private Task OnRemoteUserUpdate(MessageContainer obj)
        {
            MessageUpdateUser message = MessageUpdateUser.Unpack(obj);

            if (indicatorList.ContainsKey(message.Id))
            {
                var userIndicator = indicatorList[message.Id];
                userIndicator.SetTargetLocalPosition(message.Position);
                userIndicator.SetTargetLocalRotation(message.Orientation);
                if (userIndicator.GetComponentInChildren<Renderer>().material.color != message.Color)
                {
                    userIndicator.GetComponentInChildren<Renderer>().material.color = message.Color;
                }
            }
            else if (UserIndicatorPrefab)
            {
                // new user joined, instantiate indicator
                var userIndicator = Instantiate(UserIndicatorPrefab, worldAnchor);
                userIndicator.SetTargetLocalPosition(message.Position);
                userIndicator.SetTargetLocalRotation(message.Orientation);
                userIndicator.GetComponentInChildren<Renderer>().material.color = message.Color;
                indicatorList.Add(message.Id, userIndicator);

                // send position update, so that the new user gets up to speed
                SendUserUpdate();
            }

            return Task.CompletedTask;
        }

        private void SetColor(int i)
        {
            float hue;
            switch (i)
            {
                case 0:
                    hue = 0.0f;
                    break;

                case 1:
                    hue = 0.25f;
                    break;

                case 2:
                    hue = 0.5f;
                    break;

                default:
                    hue = UnityEngine.Random.value;
                    break;
            }

            Color = Color.HSVToRGB(hue, 0.9f, 0.95f);
        }

        // Start is called before the first frame update
        private void Start()
        {
            if (Services.VisManager() != null)
            {
                worldAnchor = Services.VisManager().WorldAnchor;
            }
            else
            {
                worldAnchor = this.transform;
            }

            SetColor(0);

            if (Services.NetworkManager())
            {
                // register network message handlers
                Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_USER, OnRemoteUserUpdate);
                Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.ACCEPT_CLIENT, OnAcceptedAsClient);
            }
            

            Transform cameraTransform = CameraCache.Main ? CameraCache.Main.transform : null;
            if (cameraTransform != null)
            {
                if (Services.NetworkManager())
                {
                    message = new MessageUpdateUser(cameraTransform.position, cameraTransform.rotation, id, Color);
                    Services.NetworkManager().SendMessage(message.Pack());
                }

                LocalUserPosition = new GameObject("LocalUserPosition").transform;
                LocalUserPosition.position = cameraTransform.position;
                LocalUserPosition.rotation = cameraTransform.rotation;
            }
        }

        // Update is called once per frame
        private void Update()
        {
            // Only consider sending an update every second frame
            // ToDo: Is this a good approach?
            if (Time.frameCount % 2 == 1)
            {
                return;
            }

            cameraTransform = CameraCache.Main ? CameraCache.Main.transform : null;
            if (Vector3.Distance(cameraTransform.position, LocalUserPosition.position) > 0.01f || Quaternion.Angle(cameraTransform.rotation, LocalUserPosition.rotation) > 0.5f)
            {
                SendUserUpdate();

            }
        }

        private void SendUserUpdate()
        {
            if(cameraTransform != null && message != null)
            {
                LocalUserPosition.position = cameraTransform.position;
                LocalUserPosition.rotation = cameraTransform.rotation;

                if (Services.NetworkManager())
                {
                    message.Color = Color;
                    message.Position = LocalUserPosition.localPosition;
                    message.Orientation = LocalUserPosition.localRotation;
                    message.Id = id;
                    Services.NetworkManager().SendMessage(message.Pack());
                }                
            }
        }
    }
}