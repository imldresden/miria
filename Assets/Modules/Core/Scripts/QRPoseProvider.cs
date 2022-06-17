using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.QR;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{

    /// <summary>
    /// A pose provider that uses QR codes.
    /// This provider is dependent on a <see cref="QRCodeManager"/> being present in the scene and will not work without one.
    /// </summary>
    public class QRPoseProvider
    {
        /// <summary>
        /// The constructor creates a new instance that watches for the provided QR code string.
        /// </summary>
        /// <param name="qrId">the data string of the QR code</param>
        public QRPoseProvider(string qrId)
        {
            this.qrId = qrId;

            SetupQRCodeManager();
        }

        public float Velocity { get; private set; }

        private Guid Id
        {
            get => id;

            set
            {
                if (id != value)
                {
                    id = value;
                    InitializeSpatialGraphNode(force: true);
                }
            }
        }

        private Guid id;
        private QRCode qrCode;
        private string qrId;
        private SpatialGraphNode node;
        private QRCodeManager qrCodeManager;
        private DateTime lastTime;
        private Pose lastPose;

        public bool GetCurrentPose(out Pose pose)
        {
            pose = default;

            if (SetupQRCodeManager() == false)
            {
                return false;
            }

            GetQRCode();

            if (node != null && node.TryLocate(FrameTime.OnUpdate, out pose))
            {
                // rotate pose because QR codes use a different coordinate system.
                pose.rotation *= Quaternion.AngleAxis(90, Vector3.right);

                var now = DateTime.Now;
                // If there is a parent to the camera that means we are using teleport and we should not apply the teleport
                // to these objects so apply the inverse
                if (CameraCache.Main.transform.parent != null)
                {
                    pose = pose.GetTransformedBy(CameraCache.Main.transform.parent);
                }

                float dS = Vector3.Distance(pose.position, lastPose.position);
                float dT = (float)(now - lastTime).TotalSeconds;
                if (dT == 0)
                {
                    Velocity = 0f;
                }
                else
                {
                    Velocity = dS / dT;
                }

                lastTime = now;
                lastPose = pose;

                return true;
            }

            return false;
        }

        private bool SetupQRCodeManager()
        {
            if (qrCodeManager == null)
            {
                qrCodeManager = QRCodeManager.FindDefaultQRCodeManager();
                if (qrCodeManager == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetQRCode()
        {
            qrCode = qrCodeManager?.GetQRCode(qrId);
            if (qrCode != null)
            {
                Id = qrCode.SpatialGraphNodeId;
            }            
        }

        private void InitializeSpatialGraphNode(bool force = false)
        {
            if (node == null || force)
            {
                node = Id != Guid.Empty ? SpatialGraphNode.FromStaticNodeId(Id) : null;
                Debug.Log("Initialize SpatialGraphNode Id= " + Id);
            }
        }
    }
}