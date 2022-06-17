//
// Based on QRCodeMiniManager:
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.QR;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace IMLD.MixedRealityAnalysis.Core
{
    public class QRCodeManager : MonoBehaviour
    {
        private Dictionary<string, QRCode> qrCodes;
        public ReadOnlyCollection<QRCode> QRCodes { get => qrCodes?.Values.ToList().AsReadOnly(); }

        /// <summary>
        /// QRCodeWatcher which will deliver notifications asynchronously.
        /// </summary>
        private QRCodeWatcher qrWatcher = null;

        /// <summary>
        /// Status of access as retrieved from user.
        /// </summary>
        private QRCodeWatcherAccessStatus accessStatus = QRCodeWatcherAccessStatus.UserPromptRequired;

        /// <summary>
        /// Whether QRCodeWatcher reports itself as supported.
        /// </summary>
        private bool isSupported = false;

        /// <summary>
        /// Get accessor for whether QRCodeWatcher reports as supported.
        /// </summary>
        public bool IsSupported => isSupported;

        /// <summary>
        /// Notification callback for a QRCode event.
        /// </summary>
        /// <param name="qrCode">The code generating the event.</param>
        /// <remarks>
        /// Note that for the enumeration complete event, qrCode parameter is always null.
        /// </remarks>
        public delegate void QRCodeFunction(QRCode qrCode);

        private QRCodeFunction onQRAdded;

        /// <summary>
        /// Callback when a new QR code is added.
        /// </summary>
        public QRCodeFunction OnQRAdded { get { return onQRAdded; } set { onQRAdded = value; } }

        private QRCodeFunction onQRUpdated;

        /// <summary>
        /// Callback when a previously added QR code is updated.
        /// </summary>
        public QRCodeFunction OnQRUpdated { get { return onQRUpdated; } set { onQRUpdated = value; } }

        private QRCodeFunction onQRRemoved;

        /// <summary>
        /// Callback when a previously added QR code is removed.
        /// </summary>
        public QRCodeFunction OnQRRemoved { get { return onQRRemoved; } set { onQRRemoved = value; } }

        private QRCodeFunction onQREnumerated;

        /// <summary>
        /// Callback when the enumeration is complete.
        /// </summary>
        /// <remarks>
        /// Cached QR codes will have Added and Updated events BEFORE the enumeration complete.
        /// Newly seen QR codes will only start to appear after the enumeration complete event.
        /// <see href="https://github.com/chgatla-microsoft/QRTracking/issues/2"/>
        /// </remarks>
        public QRCodeFunction OnQREnumerated { get { return onQREnumerated; } set { onQREnumerated = value; } }

        /// <summary>
        /// Events are stored in the PendingQRCode struct for re-issue on the main thread.
        /// </summary>
        /// <remarks>
        /// While more elegant mechanisms exist for accomplishing the same thing, the simplicity of
        /// this form provides great efficiency, especially for memory pressure.
        /// </remarks>
        private struct PendingQRCode
        {
            /// <summary>
            /// The four actions that can be taken, corresponding to the 4 subscribable delegates.
            /// </summary>
            public enum QRAction
            {
                Add,
                Update,
                Remove,
                Enumerated
            };

            /// <summary>
            /// The code which has triggered the event. For Enumerated action, qrCode will be null.
            /// </summary>
            public readonly QRCode qrCode;

            /// <summary>
            /// The type of event.
            /// </summary>
            public readonly QRAction qrAction;

            /// <summary>
            /// Constructor for immutable action.
            /// </summary>
            /// <param name="qrAction">Action to take.</param>
            /// <param name="qrCode">QR Code causing event.</param>
            public PendingQRCode(QRAction qrAction, QRCode qrCode)
            {
                this.qrAction = qrAction;
                this.qrCode = qrCode;
            }
        }
        /// <summary>
        /// Queue of qr code events to process next Update.
        /// </summary>
        private readonly Queue<PendingQRCode> pendingActions = new Queue<PendingQRCode>();

        private Task<QRCodeWatcherAccessStatus> capabilityTask;

        public QRCode GetQRCode(string qrId)
        {
            if (qrCodes != null && qrCodes.TryGetValue(qrId, out var code))
            {
                return code;
            }
            else
            {
                return null;
            }
        }

        public static QRCodeManager FindDefaultQRCodeManager()
        {
            QRCodeManager[] managers = FindObjectsOfType<QRCodeManager>();

            if (managers.Length == 0)
            {
                Debug.LogError("Unable to locate any " + typeof(QRCodeManager).FullName + " components.");
                return null;
            }
            else if (managers.Length > 1)
            {
                Debug.LogWarning("Multiple " + typeof(QRCodeManager).FullName + " components found in scene; defaulting to first available.");
            }

            return managers[0];
        }

        /// <summary>
        /// Check existence of watcher and attempt to create if needed and appropriate.
        /// </summary>
        private void CheckQRCodeWatcher()
        {
            /// Several things must happen consecutively to create the working QRCodeWatcher.
            /// 1) Access to the camera must be requested.
            /// 2) Access must have been granted.
            /// 3) And of course the whole package must be supported.
            /// When all of those conditions have been met, then we can create the watcher.
            if (qrWatcher == null && IsSupported && accessStatus == QRCodeWatcherAccessStatus.Allowed)
            {
                SetUpQRWatcher();
            }
        }

        /// <summary>
        /// Create the QRCodeWatcher instance and register for the events to be transported to the main thread.
        /// </summary>
        private void SetUpQRWatcher()
        {
            try
            {
                qrWatcher = new QRCodeWatcher();

                qrWatcher.EnumerationCompleted += OnQREnumerationEnded;
                qrWatcher.Start();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start QRCodeWatcher, error: {e.Message}");
            }
            Debug.Log($"SetUpQRWatcher {(qrWatcher != null ? "Success" : "Failed")}");
        }

        /// <summary>
        /// Deregister from the QRCodeWatcher and shut down the instance.
        /// </summary>
        private void TearDownQRWatcher()
        {
            if (capabilityTask != null)
            {
                try
                {
                    capabilityTask.Dispose();
                    capabilityTask = null;
                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogError("Cannot dispose task: " + ex.Message);
                }
            }

            if (qrWatcher != null)
            {
                qrWatcher.Stop();
                qrWatcher.Added -= OnQRCodeAddedEvent;
                qrWatcher.Updated -= OnQRCodeUpdatedEvent;
                qrWatcher.Removed -= OnQRCodeRemovedEvent;
                qrWatcher.EnumerationCompleted -= OnQREnumerationEnded;
                qrWatcher = null;
            }
        }

        /// <summary>
        /// No current action needed on enable, since setup is deferred to Update().
        /// </summary>
        private void OnEnable()
        {
        }

        /// <summary>
        /// On disable, shutdown all resources. They may be recreated on demand if re-enabled.
        /// </summary>
        private void OnDisable()
        {
            TearDownQRWatcher();
        }

        /// <summary>
        /// Record whether the QRCodeWatcher reports itself as supported, and request access.
        /// </summary>
        /// <remarks>
        /// If the camera permission has not already been granted (GetPermissions has successfully completed),
        /// then the call to QRCodeWather.RequestAccessAsync will never return, even after the user grants permissions.
        /// See https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/issues/20
        /// </remarks>
        private async void Start()
        {
            qrCodes = new Dictionary<string, QRCode>();

            isSupported = QRCodeWatcher.IsSupported();
            Debug.Log($"QRCodeWatcher.IsSupported={isSupported}");
            bool gotPermission = await GetPermissions();
            if (gotPermission)
            {
                try
                {
                    capabilityTask = QRCodeWatcher.RequestAccessAsync();
                    accessStatus = await capabilityTask;
                    Debug.Log($"Requested caps, access: {accessStatus.ToString()}");
                }
                catch (Exception ex)
                {
                    Debug.LogError("Requesting access for QR code detection failed: " + ex.Message);
                }
            }
        }

        private async Task<bool> GetPermissions()
        {
#if WINDOWS_UWP
            try
            {
                var capture = new Windows.Media.Capture.MediaCapture();
                await capture.InitializeAsync();
                Debug.Log("Camera and Microphone permissions OK");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogError("Camera and microphone permissions not granted.");
                return false;
            }
#else // WINDOWS_UWP
            await Task.CompletedTask;
            return true;
#endif // WINDOWS_UWP
        }

        private void OnApplicationQuit()
        {
            TearDownQRWatcher();
        }

        private void OnDestroy()
        {
            TearDownQRWatcher();
        }

        /// <summary>
        /// Lazily create qr code watcher resources if needed, then issue any queued events.
        /// </summary>
        private void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                TearDownQRWatcher();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                Application.Quit();
                return;
            }

            CheckQRCodeWatcher();

            lock (pendingActions)
            {
                while (pendingActions.Count > 0)
                {
                    var action = pendingActions.Dequeue();

                    switch (action.qrAction)
                    {
                        case PendingQRCode.QRAction.Add:
                            qrCodes[action.qrCode.Data] = action.qrCode;
                            AddQRCode(action.qrCode);
                            break;

                        case PendingQRCode.QRAction.Update:
                            qrCodes[action.qrCode.Data] = action.qrCode;
                            UpdateQRCode(action.qrCode);
                            break;

                        case PendingQRCode.QRAction.Remove:
                            qrCodes.Remove(action.qrCode.Data);
                            RemoveQRCode(action.qrCode);
                            break;

                        case PendingQRCode.QRAction.Enumerated:
                            QREnumerationComplete();
                            break;

                        default:
                            Debug.Assert(false, "Unknown action type");
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// Capture an Added event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="args">Args containing relevant QRCode.</param>
        private void OnQRCodeAddedEvent(object sender, QRCodeAddedEventArgs args)
        {
            //Debug.Log($"Adding {args.Code.Data}");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Add, args.Code));
            }
        }

        /// <summary>
        /// Capture an Updated event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="args">Args containing relevant QRCode.</param>
        private void OnQRCodeUpdatedEvent(object sender, QRCodeUpdatedEventArgs args)
        {
            //Debug.Log($"Updating {args.Code.Data}");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Update, args.Code));
            }
        }

        /// <summary>
        /// Capture a Removed event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="args">Args containing relevant QRCode.</param>
        private void OnQRCodeRemovedEvent(object sender, QRCodeRemovedEventArgs args)
        {
            //Debug.Log($"Removing {args.Code.Data}");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Remove, args.Code));
            }
        }

        /// <summary>
        /// Capture the Enumeration Ended event for later call on main thread.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void OnQREnumerationEnded(object sender, object e)
        {
            Debug.Log("Enumerated");
            lock (pendingActions)
            {
                pendingActions.Enqueue(new PendingQRCode(PendingQRCode.QRAction.Enumerated, null));
            }
        }

        /// <summary>
        /// Invoke Added delegate for specified qrCode.
        /// </summary>
        /// <param name="qrCode">The relevant QRCode.</param>
        private void AddQRCode(QRCode qrCode)
        {
            //Debug.Log($"Adding QR Code {qrCode.Data}");

            onQRAdded?.Invoke(qrCode);
        }

        /// <summary>
        /// Invoke Updated delegate for specified qrCode.
        /// </summary>
        /// <param name="qrCode">The relevant QRCode.</param>
        private void UpdateQRCode(QRCode qrCode)
        {
            //Debug.Log($"Updating QR Code {qrCode.Data}");

            onQRUpdated?.Invoke(qrCode);
        }

        /// <summary>
        /// Invoke Removed delegate for specified qrCode.
        /// </summary>
        /// <param name="qrCode">The relevant QRCode.</param>
        private void RemoveQRCode(QRCode qrCode)
        {
            //Debug.Log($"Removing QR Code {qrCode.Data}");

            onQRRemoved?.Invoke(qrCode);
        }

        /// <summary>
        /// Invoke Enumeration Complete delegate.
        /// </summary>
        private void QREnumerationComplete()
        {
            qrWatcher.Added += OnQRCodeAddedEvent;
            qrWatcher.Updated += OnQRCodeUpdatedEvent;
            qrWatcher.Removed += OnQRCodeRemovedEvent;

            Debug.Log($"Enumeration of QR Codes complete.");

            onQREnumerated?.Invoke(null);
        }
    }
}