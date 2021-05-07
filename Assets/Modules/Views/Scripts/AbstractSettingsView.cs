// ------------------------------------------------------------------------------------
// <copyright file="AbstractSettingsView.cs" company="Technische Universität Dresden">
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
    /// <summary>
    /// This abstract Unity component is the base for all settings views.
    /// </summary>
    public abstract class AbstractSettingsView : MonoBehaviour
    {
        /// <summary>
        /// Applies the changes to the visualization.
        /// </summary>
        public abstract void ApplyChanges();

        /// <summary>
        /// Initializes the settings view.
        /// </summary>
        /// <param name="vis">The visualization that will be configured by this settings view.</param>
        /// <param name="showStaticObjects">Whether static study objects should be shown in the settings view.</param>
        /// <param name="showSpeedSettings">Whether speed settings should be shown in the settings view.</param>
        public abstract void Init(IConfigurableVisualization vis, bool showStaticObjects = true, bool showSpeedSettings = false);
    }
}