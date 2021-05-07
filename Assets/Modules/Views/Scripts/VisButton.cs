// ------------------------------------------------------------------------------------
// <copyright file="VisButton.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using IMLD.MixedRealityAnalysis.Core;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a button used to activate or deactivate visualization.
    /// It is a toggle button, visualizing its current state and automatically shows a settings button.
    /// </summary>
    public class VisButton : MonoBehaviour
    {
        /// <summary>
        /// The id of the visualization container the visualization should be attached to or -1 if there is none.
        /// </summary>
        public int AnchorId = -2;

        /// <summary>
        /// The reference to the settings button.
        /// </summary>
        public Interactable SettingsButton;

        /// <summary>
        /// The reference to the toggle button that actually spawns the visualization.
        /// </summary>
        public Interactable SpawnButton;

        /// <summary>
        /// The type of visualization to spawn.
        /// </summary>
        public VisType VisType;

        /// <summary>
        /// Gets called when the button is toggled off.
        /// </summary>
        public void OnDisableVis()
        {
            var guidList = Services.VisManager().GetVisualizationsOfType(VisType);
            foreach (var guid in guidList)
            {
                Services.VisManager().DeleteVisualization(guid);
            }
        }

        /// <summary>
        /// Gets called when the button is toggled on.
        /// </summary>
        public void OnEnableVis()
        {
            if ((AnchorId == -1 && Services.VisManager().GetVisualizationsOfType(VisType).Count == 0) || Services.VisManager().ViewContainers[AnchorId].GetVisualizationsOfType(VisType).Count == 0)
            {
                // no visualization of this type exists
                // get all entities/datasets which are tracked objects but not touch (default)
                List<int> dataSets = new List<int>();
                for (int i = 0; i < Services.DataManager().DataSets.Count; i++)
                {
                    if (Services.DataManager().DataSets[i].ObjectType != ObjectType.TOUCH)
                    {
                        dataSets.Add(i);
                    }
                }

                Services.VisManager().CreateVisualization(
                    new VisProperties(
                        Guid.Empty,
                        VisType,
                        AnchorId,
                        dataSets,
                        new List<int>(Services.StudyManager().CurrentStudyConditions),
                        new List<int>(Services.StudyManager().CurrentStudySessions)));
            }
        }

        /// <summary>
        /// Gets called when the settings button is pressed.
        /// </summary>
        public void OnShowSettings()
        {
            var guidList = Services.VisManager().GetVisualizationsOfType(VisType);
            foreach (var guid in guidList)
            {
                Services.VisManager().OpenSettingsForVisualization(guid);
            }
        }

        /// <summary>
        /// Sets this button the on or off state. It also automatically hides the settings button when toggled off.
        /// This should be used when setting the button state without interacting with it.
        /// </summary>
        /// <param name="isActive">Whether the button should be toggled on or off.</param>
        public void SetActive(bool isActive)
        {
            SpawnButton.IsToggled = isActive;
            SettingsButton.gameObject.SetActive(isActive);
        }
    }
}