using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace IMLD.MixedRealityAnalysis.Core
{
    public class QRAnchorManager : MonoBehaviour
    {
        public static QRAnchorManager Instance = null;

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
        private ARPoseProvider _poseProvider;


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
            _poseProvider = new ARPoseProvider(QRDataString);
        }

        // Update is called once per frame
        void Update()
        {
            if (ObjectToAnchor)
            {
                bool success = _poseProvider.GetCurrentPose(out Pose pose);
                if (success)
                {
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
}