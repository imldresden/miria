//// ------------------------------------------------------------------------------------
//// <copyright file="SimpleAnchorManager.cs" company="Technische Universität Dresden">
////      Copyright (c) Technische Universität Dresden.
////      Licensed under the MIT License.
//// </copyright>
//// <author>
////      Wolfgang Büschel
//// </author>
//// <comment>
////      A simplified world anchor manager, based on the AnchorManager by Microsoft.
////      Copyright (c) Microsoft Corporation. All rights reserved.
////      Licensed under the MIT License.
//// </comment>
//// ------------------------------------------------------------------------------------

//using System;
//using System.Collections.Generic;
//using System.Net.Sockets;
//using System.Threading.Tasks;

//using IMLD.MixedRealityAnalysis.Network;
//using IMLD.MixedRealityAnalysis.Views;

//using UnityEngine;

//namespace IMLD.MixedRealityAnalysis.Core
//{
//    /// <summary>
//    /// Creates, exports, and imports anchors as required.
//    /// </summary>
//    public class SimpleAnchorManager : MonoBehaviour
//    {
//        public static SimpleAnchorManager Instance = null;

//        /// <summary>
//        /// Keeps track of the name of the world anchor to use.
//        /// </summary>
//        public string AnchorName = string.Empty;

//        /// <summary>
//        /// Gets a value indicating whether an anchor was established.
//        /// </summary>
//        public bool IsAnchorEstablished { get; private set; }

//        /// <summary>
//        /// Gets a value indicating whether an anchor is currently being imported.
//        /// </summary>
//        public bool IsImportInProgress { get; private set; }

//        /// <summary>
//        /// Gets a value indicating whether an anchor is currently being downloaded.
//        /// </summary>
//        public bool IsDownloadingAnchor { get; private set; }

//#if UNITY_WSA

//        /// <summary>
//        /// The object to attach the anchor to when created or imported.
//        /// </summary>
//        public GameObject ObjectToAnchor;

//        /// <summary>
//        /// Sometimes we'll see a really small anchor blob get generated.
//        /// These tend to not work, so we have a minimum trustworthy size.
//        /// </summary>
//        private const uint MinTrustworthySerializedAnchorDataSize = 500000;

//        /// <summary>
//        /// List of bytes that represent the anchor data to export.
//        /// </summary>
//        private readonly List<byte> exportingAnchorBytes = new List<byte>(0);

//        /// <summary>
//        /// The anchorData to import.
//        /// </summary>
//        private byte[] anchorData = null;

//        /// <summary>
//        /// Tracks if we have updated data to import.
//        /// </summary>
//        private bool gotOne = false;

//        /// <summary>
//        /// Keeps track of the name of the anchor we are exporting.
//        /// </summary>
//        private string exportingAnchorName;

//#endif

//        private void Awake()
//        {
//            // Singleton pattern implementation
//            if (Instance != null && Instance != this)
//            {
//                Destroy(gameObject);
//            }

//            Instance = this;
//        }

//        private void Start()
//        {
//#if UNITY_WSA && !(ENABLE_IL2CPP && NET_STANDARD_2_0) && !UNITY_EDITOR
//            if (HolographicSettings.IsDisplayOpaque || Services.NetworkManager() == null)
//            {
//                IsAnchorEstablished = true;
//            }
//            else
//            {
//                Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.WORLD_ANCHOR, OnAnchorData);
//                Services.NetworkManager().ClientConnected += OnClientConnected;
//            }
//#else
//            IsAnchorEstablished = true;
//#endif
//        }

//        private void OnClientConnected(object sender, NetworkManager.NewClientEventArgs e)
//        {
//            SendAnchor(e.ClientToken);
//        }

//        private void Update()
//        {
//#if UNITY_WSA
//#if UNITY_2017_2_OR_NEWER
//            if (HolographicSettings.IsDisplayOpaque)
//            {
//                return;
//            }
//#else
//        if (!VRDevice.isPresent)
//        {
//            return;
//        }
//#endif

//            if (gotOne)
//            {
//                _ = ProgressIndicator.StartProgressIndicator("Importing anchor data...");
//                Debug.Log("Importing");
//                gotOne = false;
//                IsImportInProgress = true;
//                WorldAnchorTransferBatch.ImportAsync(anchorData, ImportComplete);
//            }
//#endif
//        }

//        /// <summary>
//        /// Sends the anchor to a client.
//        /// </summary>
//        /// <param name="client">The client connection to send the anchor to.</param>
//        public void SendAnchor(Guid client)
//        {
//#if UNITY_WSA && !(ENABLE_IL2CPP && NET_STANDARD_2_0) && !UNITY_EDITOR
//        if (exportingAnchorBytes != null && IsAnchorEstablished && Services.NetworkManager() != null)
//        {
//            // Send existing anchor data to clients
//            var Command = new MessageWorldAnchor(exportingAnchorBytes.ToArray());
//            Services.NetworkManager().SendMessage(Command.Pack(), client);
//        }
//        else
//        {
//            // create new anchor and send it to clients
//            CreateAnchor();
//        }
//#endif
//        }

//#if UNITY_WSA

//        private Task OnAnchorData(MessageContainer container)
//        {
//            MessageWorldAnchor message = MessageWorldAnchor.Unpack(container);
//            Debug.Log("Anchor data arrived.");
//            anchorData = message.AnchorData;
//            Debug.Log(anchorData.Length);
//            IsDownloadingAnchor = false;
//            gotOne = true;
//            return Task.CompletedTask;
//        }

//        /// <summary>
//        /// If we are supposed to create the anchor for export, this is the function to call.
//        /// </summary>
//        public void CreateAnchor()
//        {
//            ExportAnchorAtPosition();
//        }

//        /// <summary>
//        /// Creates and exports the anchor at the specified world position
//        /// </summary>
//        private void ExportAnchorAtPosition()
//        {
//            // Need to remove any anchor that is on the object before we can move the object.
//            WorldAnchor worldAnchor = ObjectToAnchor.GetComponent<WorldAnchor>();
//            if (worldAnchor != null)
//            {
//                DestroyImmediate(worldAnchor);
//            }

//            // Attach a new anchor
//            worldAnchor = ObjectToAnchor.AddComponent<WorldAnchor>();

//            // Name the anchor
//            exportingAnchorName = Guid.NewGuid().ToString();
//            Debug.Log("preparing " + exportingAnchorName);

//            // Register for on tracking changed in case the anchor isn't already located
//            worldAnchor.OnTrackingChanged += WorldAnchor_OnTrackingChanged;

//            // And call our callback in line just in case it is already located.
//            WorldAnchor_OnTrackingChanged(worldAnchor, worldAnchor.isLocated);
//        }

//        /// <summary>
//        /// Callback for when tracking changes for an anchor
//        /// </summary>
//        /// <param name="self">The anchor that tracking has changed for.</param>
//        /// <param name="located">Bool if the anchor is located</param>
//        private void WorldAnchor_OnTrackingChanged(WorldAnchor self, bool located)
//        {
//            if (located)
//            {
//                // If we have located the anchor we can export it.
//                Debug.Log("exporting " + exportingAnchorName);

//                // And we don't need this callback anymore
//                self.OnTrackingChanged -= WorldAnchor_OnTrackingChanged;

//                ExportAnchor();
//            }
//        }

//        /// <summary>
//        /// Exports the anchor on the ObjectToAnchor.
//        /// </summary>
//        private void ExportAnchor()
//        {
//            WorldAnchorTransferBatch watb = new WorldAnchorTransferBatch();
//            WorldAnchor worldAnchor = ObjectToAnchor.GetComponent<WorldAnchor>();
//            watb.AddWorldAnchor(exportingAnchorName, worldAnchor);
//            WorldAnchorTransferBatch.ExportAsync(watb, WriteBuffer, ExportComplete);
//        }

//        /// <summary>
//        /// Called when a remote anchor has been deserialized
//        /// </summary>
//        /// <param name="status">Tracks if the import worked</param>
//        /// <param name="wat">The WorldAnchorTransferBatch that has the anchor information.</param>
//        private void ImportComplete(SerializationCompletionReason status, WorldAnchorTransferBatch wat)
//        {
//            _ = ProgressIndicator.StopProgressIndicator();
//            if (status == SerializationCompletionReason.Succeeded && wat.GetAllIds().Length > 0)
//            {
//                Debug.Log("Import complete");

//                string first = wat.GetAllIds()[0];
//                Debug.Log("Anchor name: " + first);

//                WorldAnchor existingAnchor = ObjectToAnchor.GetComponent<WorldAnchor>();
//                if (existingAnchor != null)
//                {
//                    DestroyImmediate(existingAnchor);
//                }

//                WorldAnchor anchor = wat.LockObject(first, ObjectToAnchor);
//                anchor.OnTrackingChanged += Anchor_OnTrackingChanged;
//                Anchor_OnTrackingChanged(anchor, anchor.isLocated);

//                IsImportInProgress = false;
//            }
//            else
//            {
//                // if we failed, we can simply try again.
//                gotOne = true;
//                Debug.Log("Import fail");
//            }
//        }

//        private void Anchor_OnTrackingChanged(WorldAnchor self, bool located)
//        {
//            if (located)
//            {
//                IsAnchorEstablished = true;
//                ////WorldAnchorManager.Instance.AnchorStore.Save(AnchorName, self);
//                self.OnTrackingChanged -= Anchor_OnTrackingChanged;
//            }
//        }

//        /// <summary>
//        /// Called as anchor data becomes available to export
//        /// </summary>
//        /// <param name="data">The next chunk of data.</param>
//        private void WriteBuffer(byte[] data)
//        {
//            exportingAnchorBytes.AddRange(data);
//        }

//        /// <summary>
//        /// Called when serializing an anchor is complete.
//        /// </summary>
//        /// <param name="status">If the serialization succeeded.</param>
//        private void ExportComplete(SerializationCompletionReason status)
//        {
//            if (status == SerializationCompletionReason.Succeeded && exportingAnchorBytes.Count > MinTrustworthySerializedAnchorDataSize)
//            {
//                AnchorName = exportingAnchorName;
//                anchorData = exportingAnchorBytes.ToArray();
//                ////createdAnchor = true;
//                Debug.Log("Anchor ready " + exportingAnchorBytes.Count);
//                IsAnchorEstablished = true;

//                // Send anchor data to clients
//                if (Services.NetworkManager() != null)
//                {
//                    var command = new MessageWorldAnchor(exportingAnchorBytes.ToArray());
//                    Services.NetworkManager().SendMessage(command.Pack());
//                }

//            }
//            else
//            {
//                Debug.Log("Create anchor failed " + status + " " + exportingAnchorBytes.Count);
//                exportingAnchorBytes.Clear();
//                DestroyImmediate(ObjectToAnchor.GetComponent<WorldAnchor>());
//                CreateAnchor();
//            }
//        }

//#endif
//    }
//}