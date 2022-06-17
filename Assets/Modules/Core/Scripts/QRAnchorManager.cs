using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    public class QRAnchorManager : MonoBehaviour
    {
        public static QRAnchorManager Instance = null;

        /// <summary>
        /// Keeps track of the name of the anchor to use.
        /// </summary>
        public string AnchorName = string.Empty;

        /// <summary>
        /// Gets a value indicating whether an anchor was established.
        /// </summary>
        public bool IsAnchorEstablished { get; private set; }

        /// <summary>
        /// The object to attach the anchor to when created or imported.
        /// </summary>
        public GameObject ObjectToAnchor;

        private QRPoseProvider _poseProvider;

        // Start is called before the first frame update
        void Start()
        {
            _poseProvider = new QRPoseProvider(AnchorName);
        }

        // Update is called once per frame
        void Update()
        {
            if (ObjectToAnchor)
            {
                bool success = _poseProvider.GetCurrentPose(out Pose pose);
                if (success)
                {
                    ObjectToAnchor.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }                
            }
        }
    }
}