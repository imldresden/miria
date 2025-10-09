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
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    // TODO: MRTKv3 migration

    /// <summary>
    /// This Unity component is a button used in the session manager view to start a new analysis session.
    /// </summary>
    public class StartSessionButton : MonoBehaviour
    {
        /// <summary>
        /// Implements <see cref="IMixedRealityPointerHandler"/>. Starts a new analysis session.
        /// </summary>
        /// <param name="eventData">The click event data.</param>
        public void OnPointerClicked()
        {
            Services.NetworkManager().StartAsServer();
            // eventData.Use();
        }
    }
}