// ------------------------------------------------------------------------------------
// <copyright file="StartSessionButton.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using IMLD.MixedRealityAnalysis.Core;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a button used in the session manager view to start a new analysis session.
    /// </summary>
    public class StartSessionButton : MonoBehaviour, IMixedRealityPointerHandler
    {
        /// <summary>
        /// Implements <see cref="IMixedRealityPointerHandler"/>. Starts a new analysis session.
        /// </summary>
        /// <param name="eventData">The click event data.</param>
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            Services.NetworkManager().StartAsServer();
            eventData.Use();
        }

        /// <summary>
        /// Implements <see cref="IMixedRealityPointerHandler"/>. Has no function.
        /// </summary>
        /// <param name="eventData">The pointer down event data.</param>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
        }

        /// <summary>
        /// Implements <see cref="IMixedRealityPointerHandler"/>. Has no function.
        /// </summary>
        /// <param name="eventData">The pointer dragged event data.</param>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        /// <summary>
        /// Implements <see cref="IMixedRealityPointerHandler"/>. Has no function.
        /// </summary>
        /// <param name="eventData">The pointer up event data.</param>
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
        }
    }
}