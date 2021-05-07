// ------------------------------------------------------------------------------------
// <copyright file="SimpleVisualizationSettingsView.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Collections.Generic;
using IMLD.MixedRealityAnalysis.Core;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a basic implementation of the <see cref="AbstractSettingsView"/>.
    /// </summary>
    public class SimpleVisualizationSettingsView : AbstractSettingsView
    {
        public SettingsViewObject SettingsPrefab;

        private const float OffsetY = 0.08f;
        private const float StartPositionX = -0.3f;
        private const float StartPositionY = 0.1f;
        private VisProperties settings;
        private List<SettingsViewObject> settingsViewObjects;
        private bool showSpeedSettings = false;
        private bool showStaticObjects = true;

        /// <summary>
        /// Applies the changes from this settings view to the visualization.
        /// </summary>
        public override void ApplyChanges()
        {
            List<int> dataSets = new List<int>();
            List<bool> useSpeed = new List<bool>();

            // generate list of objects to show in trajectory view
            foreach (var settingsViewObject in settingsViewObjects)
            {
                if (settingsViewObject.IsObjectSelected)
                {
                    dataSets.Add(settingsViewObject.DataSet.Id);
                    if (showSpeedSettings)
                    {
                        useSpeed.Add(settingsViewObject.IsUseSpeedSelected);
                    }
                }
            }

            var config = new VisProperties(settings.VisId, settings.VisType, settings.AnchorId, dataSets, new List<int>(Services.StudyManager().CurrentStudyConditions), new List<int>(Services.StudyManager().CurrentStudySessions));
            
            if (showSpeedSettings)
            {
                config.Set("useSpeed", useSpeed);
            }

            // update settings of visualization, also triggers update over network
            Services.VisManager().UpdateVisualization(config);
            Close();
        }

        /// <summary>
        /// Closes the settings view.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        /// <summary>
        /// Initializes the settings view.
        /// </summary>
        /// <param name="vis">The visualization that will be configured by this settings view.</param>
        /// <param name="showStaticObjects">Whether static study objects should be shown in the settings view.</param>
        /// <param name="showSpeedSettings">Whether speed settings should be shown in the settings view.</param>
        public override void Init(IConfigurableVisualization vis, bool showStaticObjects = true, bool showSpeedSettings = false)
        {
            if (vis == null)
            {
                return;
            }

            settings = vis.Settings;
            settingsViewObjects = new List<SettingsViewObject>();

            this.showStaticObjects = showStaticObjects;
            this.showSpeedSettings = showSpeedSettings;

            int i = 0;

            // create settings prefab for each object
            if (SettingsPrefab && Services.DataManager() != null)
            {
                foreach (var dataSet in Services.DataManager().DataSets.Values)
                {
                    if (this.showStaticObjects || dataSet.IsStatic == false)
                    {
                        var settingsViewObject = GameObject.Instantiate<SettingsViewObject>(SettingsPrefab, this.transform);
                        settingsViewObject.transform.localPosition = new Vector3(StartPositionX, StartPositionY - (i * OffsetY), -0.009f);
                        settingsViewObject.DataSet = dataSet;
                        settingsViewObject.Init(this.showSpeedSettings);
                        for (int idx = 0; idx < settings.ObjectIds.Count; idx++)
                        {
                            if (settings.ObjectIds[idx] == dataSet.Id)
                            {
                                settingsViewObject.IsObjectSelected = true;
                                if (this.showSpeedSettings)
                                {
                                    if (settings.TryGet("useSpeed", out List<bool> useSpeedList))
                                    {
                                        if (useSpeedList != null && useSpeedList.Count == settings.ObjectIds.Count)
                                        {
                                            settingsViewObject.IsUseSpeedSelected = useSpeedList[idx];
                                        }
                                    }
                                }
                            }
                        }

                        settingsViewObjects.Add(settingsViewObject);
                        i++;
                    }
                }
            }

            Transform cameraTransform = CameraCache.Main ? CameraCache.Main.transform : null;
            if (cameraTransform != null)
            {
                transform.position = cameraTransform.position + (0.5f * cameraTransform.forward);
                ////transform.LookAt(cameraTransform.position, Vector3.up);
                Quaternion rotation = Quaternion.LookRotation(cameraTransform.position + cameraTransform.forward, Vector3.up);
                transform.rotation = rotation;
            }
        }
    }
}