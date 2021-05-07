// ------------------------------------------------------------------------------------
// <copyright file="JoinSessionButton.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a button used in the session view.
    /// </summary>
    public class JoinSessionButton : MonoBehaviour, IMixedRealityPointerHandler
    {
        /// <summary>
        /// Implements IMixedRealityPointerHandler. Joins the selected session.
        /// </summary>
        /// <param name="eventData">The event data for the click event.</param>
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            SessionListUIController.Instance.JoinSelectedSession();
            eventData.Use();
        }

        /// <summary>
        /// Implements IMixedRealityPointerHandler. Has no function.
        /// </summary>
        /// <param name="eventData">The event data for the pointer down event.</param>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
        }

        /// <summary>
        /// Implements IMixedRealityPointerHandler. Has no function.
        /// </summary>
        /// <param name="eventData">The event data for the pointer dragged event.</param>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        /// <summary>
        /// Implements IMixedRealityPointerHandler. Has no function.
        /// </summary>
        /// <param name="eventData">The event data for the pointer up event.</param>
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
        }
    }
}