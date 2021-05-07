// --------------------------------------------------------------------------------------------------
// <copyright file="TrajectoryVisualizationSettingsView.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// --------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using IMLD.MixedRealityAnalysis.Core;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component implements the <see cref="AbstractSettingsView"/> for trajectory visualizations.
    /// </summary>
    public class TrajectoryVisualizationSettingsView : AbstractSettingsView
    {
        public SettingsViewObject SettingsPrefab;

        private const float OffsetY = 0.08f;
        private const float StartPositionX = -0.3f;
        private const float StartPositionY = 0.1f;
        private List<SettingsViewObject> settingsViewObjects;
        private Guid visId;

        /// <summary>
        /// Applies the changes to the visualization.
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
                    Debug.Log(settingsViewObject.gameObject.name);
                    dataSets.Add(settingsViewObject.DataSet.Id);
                    useSpeed.Add(settingsViewObject.IsUseSpeedSelected);
                }
            }

            var config = new VisProperties(visId, (int)VisType.Trajectory3D, -1, dataSets, new List<int>(Services.StudyManager().CurrentStudyConditions), new List<int>(Services.StudyManager().CurrentStudySessions));
            config.Set("useSpeed", useSpeed);

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

            VisProperties settings = vis.Settings;
            visId = vis.Settings.VisId;
            settingsViewObjects = new List<SettingsViewObject>();

            int i = 0;

            // create settings prefab for each object
            if (SettingsPrefab && Services.DataManager() != null)
            {
                foreach (var dataSetElem in Services.DataManager().DataSets.Values)
                {
                    AnalysisObject dataSet = (AnalysisObject)dataSetElem;
                    var settingsViewObject = GameObject.Instantiate<SettingsViewObject>(SettingsPrefab, this.transform);
                    settingsViewObject.transform.localPosition = new Vector3(StartPositionX, StartPositionY - (i * OffsetY), -0.009f);
                    settingsViewObject.DataSet = dataSet;
                    settingsViewObject.Init();
                    for (int idx = 0; idx < settings.ObjectIds.Count; idx++)
                    {
                        if (settings.ObjectIds[idx] == dataSetElem.Id)
                        {
                            settingsViewObject.IsObjectSelected = true;
                            if (settings.TryGet("useSpeed", out List<bool> useSpeedList))
                            {
                                if (useSpeedList != null && useSpeedList.Count == settings.ObjectIds.Count)
                                {
                                    settingsViewObject.IsUseSpeedSelected = useSpeedList[idx];
                                }
                            }
                        }
                    }

                    settingsViewObjects.Add(settingsViewObject);
                    i++;
                }
            }
        }
    }
}