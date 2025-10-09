// ------------------------------------------------------------------------------------
// <copyright file="JoinSessionButton.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    // TODO: MRTKv3 migration

    /// <summary>
    /// This Unity component is a button used in the session view.
    /// </summary>
    public class JoinSessionButton : MonoBehaviour
    {
        /// <summary>
        /// Implements IMixedRealityPointerHandler. Joins the selected session.
        /// </summary>
        /// <param name="eventData">The event data for the click event.</param>
        public void OnPointerClicked()
        {
            SessionListUIController.Instance.JoinSelectedSession();
        }
    }
}