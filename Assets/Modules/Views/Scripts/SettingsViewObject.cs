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
using IMLD.MixedRealityAnalysis.Utils;
using MixedReality.Toolkit.UX;
using TMPro;
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

        
        public PressableButton SelectionCheckbox;
        public PressableButton SpeedCheckbox;
        public TextMeshProUGUI PropertyName;

        private bool isInitialized = false;

        
        /// <summary>
        /// Gets or sets a value indicating whether the study object is selected.
        /// </summary>
        public bool IsObjectSelected { get => SelectionCheckbox.IsToggled; set => SelectionCheckbox.ForceSetToggled(value, false); }

        /// <summary>
        /// Gets or sets a value indicating whether using speed for the object is selected.
        /// </summary>
        public bool IsUseSpeedSelected { get => SpeedCheckbox.IsToggled; set => SpeedCheckbox.ForceSetToggled(value, false); }

        /// <summary>
        /// Initializes this object.
        /// </summary>
        /// <param name="showSpeedSettings">This determines if the "using speed" option should be shown.</param>
        public void Init(bool showSpeedSettings = false)
        {
            PropertyName.SetText(DataSet.Title);

            //Renderer.material.color = DataSet.ObjectColor;

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