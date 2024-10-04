using Microsoft.MixedReality.OpenXR;
using System;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    public class ARPoseProvider
    {
        public float Velocity { get; private set; }

        public string MarkerString { get; set; }

        private ARMarkerManager _markerManager;
        private ARMarker _marker;
        private DateTime _lastTime;
        private Pose _lastPose;
        private Camera _cachedCamera;
        public ARPoseProvider(string markerString)
        {
            MarkerString = markerString;
        }

        public bool GetCurrentPose(out Pose pose)
        {
            pose = default;

            if (SetupARMarkerManager() == false)
            {
                return false;
            }
            GetARMarker();

            if (_marker != null)
            {
                pose = new Pose(_marker.transform.position, _marker.transform.rotation);
                var now = DateTime.Now;
                // If there is a parent to the camera that means we are using teleport and we should not apply the teleport
                // to these objects so apply the inverse
                if (Camera.transform.parent != null)
                {
                    pose = pose.GetTransformedBy(Camera.transform.parent);
                }

                float dS = Vector3.Distance(pose.position, _lastPose.position);
                float dT = (float)(now - _lastTime).TotalSeconds;
                if (dT == 0)
                {
                    Velocity = 0f;
                }
                else
                {
                    Velocity = dS / dT;
                }

                _lastTime = now;
                _lastPose = pose;

                return true;
            }

            return false;
        }

        private bool SetupARMarkerManager()
        {
            if (_markerManager == null)
            {
                _markerManager = GameObject.FindObjectOfType<ARMarkerManager>();
                if (_markerManager == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetARMarker()
        {
            foreach (var trackable in _markerManager.trackables)
            {
                if (MarkerString == "" || trackable.GetDecodedString() == MarkerString)
                {
                    _marker = trackable;
                    return;
                }
            }
            _marker = null;
        }

        private Camera Camera
        {
            get
            {
                if (_cachedCamera != null && _cachedCamera.gameObject.activeInHierarchy)
                {
                    return _cachedCamera;
                }

                Camera camera = Camera.main;
                if (camera == null)
                {
                    var obj = GameObject.FindWithTag("MainCamera");
                    if (obj == null || !obj.TryGetComponent(out camera))
                    {
                        Debug.LogError("No main camera found! Creating camera object");
                        camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener))
                        {
                            tag = "MainCamera"
                        }.GetComponent<Camera>();
                    }
                }

                _cachedCamera = camera;
                return _cachedCamera;
            }
        }
    }
}
