// ------------------------------------------------------------------------------------
// <copyright file="IConfigurableVisualization.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using IMLD.MixedRealityAnalysis.Core;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// Interface for visualizations that can be configured.
    /// </summary>
    public interface IConfigurableVisualization
    {
        /// <summary>
        /// Gets the current visualization settings.
        /// </summary>
        VisProperties Settings { get; }

        /// <summary>
        /// Opens the settings user interface for the visualization.
        /// </summary>
        void OpenSettingsUI();
    }
}