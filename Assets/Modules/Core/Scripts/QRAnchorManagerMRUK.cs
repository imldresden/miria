using Meta.XR.MRUtilityKit;
using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using static OVRPlugin;

namespace IMLD.MixedRealityAnalysis.Core
{
    public class QRAnchorManagerMRUK : MonoBehaviour
    {
        public static QRAnchorManagerMRUK Instance = null;

        /// <summary>
        /// The text of the QR code that should be tracked. If empty, all (any) QR code found in the environment is used.
        /// </summary>
        [Tooltip("The text of the QR code that should be tracked. If empty, all (any) QR code found in the environment is used.")]
        public string QRDataString = string.Empty;

        /// <summary>
        /// The game object that should be anchored at the QR code's position.
        /// </summary>
        [Tooltip("The game object that should be anchored at the QR code's position.")]
        public GameObject ObjectToAnchor;

        [Tooltip("Whether or not to use AR anchors to stabilize the position.")]
        public bool IncreaseStability;

        /// <summary>
        /// Gets a value indicating whether an anchor was established.
        /// </summary>
        public bool IsAnchorEstablished { get; private set; }

        public Vector3 RotationOffset;

        private ARAnchor _anchor;

        private MRUKTrackable _trackable;


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
            var Mruk = FindAnyObjectByType<MRUK>();
            if (Mruk != null)
            {
                Mruk.SceneSettings.TrackableAdded.AddListener(TrackableAdded);
                Mruk.SceneSettings.TrackableRemoved.AddListener(TrackableRemoved);
            }
        }

        private void TrackableAdded(MRUKTrackable trackable)
        {
            if (trackable.MarkerPayloadString == QRDataString)
            {
                _trackable = trackable;
            }
        }

        private void TrackableRemoved(MRUKTrackable trackable)
        {
            if (trackable.MarkerPayloadString == QRDataString && _trackable != null)
            {
                _trackable = null;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (ObjectToAnchor != null && _trackable != null)
            {
                Pose pose = new Pose(_trackable.transform.position, _trackable.transform.rotation);

                if (IncreaseStability)
                {
                    if (Vector3.Distance(pose.position, ObjectToAnchor.transform.position) > 0.02f)
                    {
                        // delete old world anchor
                        if (_anchor)
                        {
                            DestroyImmediate(_anchor);
                        }

                        // reposition object
                        ObjectToAnchor.transform.SetPositionAndRotation(pose.position, pose.rotation * Quaternion.Euler(RotationOffset));

                        // create new anchor
                        _anchor = ObjectToAnchor.AddComponent<ARAnchor>();
                    }
                }
                else
                {
                    // delete old world anchor
                    if (_anchor)
                    {
                        DestroyImmediate(_anchor);
                    }

                    // reposition object
                    ObjectToAnchor.transform.SetPositionAndRotation(pose.position, pose.rotation * Quaternion.Euler(RotationOffset));
                }
            }
        }
    }
}