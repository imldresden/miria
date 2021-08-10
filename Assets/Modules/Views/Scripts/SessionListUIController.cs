// ------------------------------------------------------------------------------------
// <copyright file="SessionListUIController.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// <comment>
//      Based on code by Microsoft.
//      Copyright (c) Microsoft Corporation. All rights reserved.
//      Licensed under the MIT License.
// </comment>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using IMLD.MixedRealityAnalysis.Core;
using IMLD.MixedRealityAnalysis.Network;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// Controls a scrollable list of sessions.
    /// </summary>
    public class SessionListUIController : MonoBehaviour
    {
        public static SessionListUIController Instance = null;

        /// <summary>
        /// List of session controls that will have the session info on them.
        /// </summary>
        public SessionListButton[] SessionControls;

        private NetworkManager networkManager;

        /// <summary>
        /// Keeps track of the current index that is the 'top' of the UI list
        /// to enable scrolling.
        /// </summary>
        private int sessionIndex = 0;

        /// <summary>
        /// Current list of sessions.
        /// TODO: Currently these don't clean up if a session goes away...
        /// </summary>
        private Dictionary<string, NetworkManager.SessionInfo> sessionList;

        /// <summary>
        /// Gets the session the user has currently selected.
        /// </summary>
        public NetworkManager.SessionInfo SelectedSession { get; private set; }

        /// <summary>
        /// Joins the selected session if there is a selected session.
        /// </summary>
        public void JoinSelectedSession()
        {
            if (SelectedSession != null)
            {
                networkManager.JoinSession(SelectedSession);
            }
        }

        /// <summary>
        /// Updates which session is the 'top' session in the list, and sets the
        /// session buttons accordingly
        /// </summary>
        /// <param name="direction">are we scrolling up, down, or not scrolling</param>
        public void ScrollSessions(int direction)
        {
            int sessionCount = sessionList == null ? 0 : sessionList.Count;

            // Update the session index
            sessionIndex = Mathf.Clamp(sessionIndex + direction, 0, Mathf.Max(0, sessionCount - SessionControls.Length));

            // Update all of the controls
            for (int index = 0; index < SessionControls.Length; index++)
            {
                if (sessionIndex + index < sessionCount)
                {
                    SessionControls[index].gameObject.SetActive(true);
                    NetworkManager.SessionInfo sessionInfo = sessionList.Values.ElementAt(sessionIndex + index);
                    SessionControls[index].SetSessionInfo(sessionInfo);
                }
                else
                {
                    SessionControls[index].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Sets the selected session
        /// </summary>
        /// <param name="sessionInfo">The session to set as selected</param>
        public void SetSelectedSession(NetworkManager.SessionInfo sessionInfo)
        {
            SelectedSession = sessionInfo;

            // Recalculating the session list so we can update the text colors.
            ScrollSessions(0);
        }

        /// <summary>
        /// Starts a new session.
        /// </summary>
        public void StartSession()
        {
            networkManager.StartAsServer();
        }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }

        /// <summary>
        /// When we are connected we want to disable the UI
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">the event data</param>
        private void NetworkDiscovery_ConnectionStatusChanged(object sender, EventArgs e)
        {
            // Hide this dialog if either is true: 1. we are a server or 2. we are a client and we are connected to a server
            gameObject.SetActive(!networkManager.IsServer && !networkManager.IsConnected);
        }

        /// <summary>
        /// Called when a session is discovered
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">the event data</param>
        private void NetworkDiscovery_SessionListChanged(object sender, EventArgs e)
        {
            sessionList = networkManager.Sessions;
            sessionIndex = Mathf.Min(sessionIndex, sessionList.Count);

            // this will force a recalculation of the buttons.
            ScrollSessions(0);
        }

        private void Start()
        {
            // Register for events when sessions are found / joined.
            networkManager = Services.NetworkManager();
            networkManager.SessionListChanged += NetworkDiscovery_SessionListChanged;
            networkManager.ConnectionStatusChanged += NetworkDiscovery_ConnectionStatusChanged;
            ScrollSessions(0);
        }
    }
}