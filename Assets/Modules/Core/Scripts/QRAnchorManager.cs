using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

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
        private ARAnchor _anchor;

        private QRPoseProvider _poseProvider;


        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

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
                    if (Vector3.Distance(pose.position, ObjectToAnchor.transform.position) > 0.02f)
                    {
                        // delete old world anchor
                        if (_anchor)
                        {
                            DestroyImmediate(_anchor);
                        }

                        // reposition object
                        ObjectToAnchor.transform.SetPositionAndRotation(pose.position, pose.rotation);

                        // create new anchor
                        _anchor = ObjectToAnchor.AddComponent<ARAnchor>();
                    }
                }                
            }
        }
    }
}