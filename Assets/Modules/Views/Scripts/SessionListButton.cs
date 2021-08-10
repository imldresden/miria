// ------------------------------------------------------------------------------------
// <copyright file="SessionListButton.cs" company="Technische Universität Dresden">
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

using IMLD.MixedRealityAnalysis.Network;
using TMPro;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a button in the session list. Each button represents one joinable session.
    /// </summary>
    public class SessionListButton : MonoBehaviour
    {
        public TextMeshPro TextMesh;

        /// <summary>
        /// Information about the session attached to this button
        /// </summary>
        private NetworkManager.SessionInfo sessionInfo;

        /// <summary>
        /// The material for the text so we can change the text color.
        /// </summary>
        private Material textMaterial;

        /// <summary>
        /// When the user clicks a session this will route that information to the
        /// scrolling UI control so it knows which session is selected.
        /// </summary>
        public void OnClicked()
        {
            SessionListUIController.Instance.SetSelectedSession(sessionInfo);
        }

        /// <summary>
        /// Sets the session information associated with this button
        /// </summary>
        /// <param name="sessionInfo">The session info</param>
        public void SetSessionInfo(NetworkManager.SessionInfo sessionInfo)
        {
            this.sessionInfo = sessionInfo;
            if (this.sessionInfo != null)
            {
                TextMesh.text = string.Format("{0}\n{1}", this.sessionInfo.SessionName, this.sessionInfo.SessionIp);
                if (this.sessionInfo == SessionListUIController.Instance.SelectedSession)
                {
                    TextMesh.GetComponent<MeshRenderer>().material.SetColor(Shader.PropertyToID("_Color"), Color.blue);

                    TextMesh.color = Color.blue;
                }
                else
                {
                    TextMesh.GetComponent<MeshRenderer>().material.SetColor(Shader.PropertyToID("_Color"), Color.yellow);
                    TextMesh.color = Color.yellow;
                }
            }
        }

        /// <summary>
        /// Called by unity when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (textMaterial != null)
            {
                Destroy(textMaterial);
                textMaterial = null;
            }
        }
    }
}