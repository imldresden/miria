// ------------------------------------------------------------------------------------
// <copyright file="SettingsViewObject.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using IMLD.MixedRealityAnalysis.Core;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component represents a study object in the settings views.
    /// </summary>
    public class SettingsViewObject : MonoBehaviour
    {
        public AnalysisObject DataSet;
        public MeshRenderer Renderer;
        public Interactable SelectionCheckbox;
        public Interactable SpeedCheckbox;
        private bool isInitialized = false;

        /// <summary>
        /// Gets or sets a value indicating whether the study object is selected.
        /// </summary>
        public bool IsObjectSelected { get => SelectionCheckbox.IsToggled; set => SelectionCheckbox.IsToggled = value; }

        /// <summary>
        /// Gets or sets a value indicating whether using speed for the object is selected.
        /// </summary>
        public bool IsUseSpeedSelected { get => SpeedCheckbox.IsToggled; set => SpeedCheckbox.IsToggled = value; }

        /// <summary>
        /// Initializes this object.
        /// </summary>
        /// <param name="showSpeedSettings">This determines if the "using speed" option should be shown.</param>
        public void Init(bool showSpeedSettings = false)
        {
            var helper = SelectionCheckbox.gameObject.GetComponent<ButtonConfigHelper>();
            if (helper)
            {
                helper.MainLabelText = DataSet.Title;
            }

            Renderer.material.color = DataSet.ObjectColor;

            if (!showSpeedSettings)
            {
                SpeedCheckbox.gameObject.SetActive(false);
            }
            else
            {
                SpeedCheckbox.gameObject.SetActive(true);
            }

            isInitialized = true;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!isInitialized)
            {
                Init();
                isInitialized = true;
            }
        }
    }
}