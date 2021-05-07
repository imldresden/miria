// ------------------------------------------------------------------------------------
// <copyright file="Vis3DModels.cs" company="Technische Universität Dresden">
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
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component visualizes 3D models. It implements <see cref="AbstractView"/> and <see cref="IConfigurableVisualization"/>.
    /// </summary>
    public class Vis3DModels : AbstractView, IConfigurableVisualization
    {
        /// <summary>
        /// The prefab for the rendering of a 3D object.
        /// </summary>
        public GameObject MarkerPrefab;

        /// <summary>
        /// The prefab for the settings view.
        /// </summary>
        public AbstractSettingsView SettingsViewPrefab;

        /// <summary>
        /// A list of the current analysis objects.
        /// </summary>
        protected List<AnalysisObject> dataSets = new List<AnalysisObject>();

        private bool isInitialized = false;
        private readonly List<GameObject> markers = new List<GameObject>();

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => true;

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.Model3D;

        /// <summary>
        /// Initializes the visualization with the provided settings.
        /// </summary>
        /// <param name="settings">The settings to use for this visualization.</param>
        public override void Init(VisProperties settings)
        {
            if (isInitialized)
            {
                Reset(); // reset if we are already initialized
            }

            if (Services.DataManager() == null || Services.DataManager().CurrentStudy == null)
            {
                // no study loaded
                return; // vis is now reset, we return because there is nothing to load
            }

            Settings = ParseSettings(settings); // parse the settings from the settings object, also makes a deep copy

            VisId = Settings.VisId;
            foreach (var dataSetIndex in Settings.ObjectIds)
            {
                dataSets.Add(Services.DataManager().DataSets[dataSetIndex]);
            }

            DrawGraph();

            isInitialized = true;
        }

        /// <summary>
        /// Opens the settings user interface for the visualization.
        /// </summary>
        public void OpenSettingsUI()
        {
            AbstractSettingsView settingsView = Instantiate(SettingsViewPrefab);
            settingsView.Init(this, true, false);
            settingsView.gameObject.SetActive(true);
        }

        /// <summary>
        /// Updates the view.
        /// </summary>
        public override void UpdateView()
        {
            Init(Settings);
        }

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public override void UpdateView(VisProperties settings)
        {
            Init(settings);
        }

        private void AddMarker(Mesh objectModel, Color color, int dataSetId, int sessionId, int conditionId)
        {
            var dataSet = Services.DataManager().DataSets[dataSetId];
            GameObject marker = Instantiate(MarkerPrefab, Anchor);
            MeshFilter filter = marker.GetComponent<MeshFilter>();
            filter.mesh = objectModel;
            Renderer renderer = marker.GetComponent<Renderer>();
            TrajectoryMarker markerData = marker.GetComponent<TrajectoryMarker>();
            markerData.DataSetId = dataSetId;
            markerData.SessionId = sessionId;
            markerData.ConditionId = conditionId;
            marker.transform.localPosition = dataSet.LocalPosition;
            marker.transform.localRotation = dataSet.LocalRotation;
            marker.transform.localScale = new Vector3(marker.transform.localScale.x * dataSet.LocalScale.x, marker.transform.localScale.y * dataSet.LocalScale.y, marker.transform.localScale.z * dataSet.LocalScale.z);
            renderer.material.color = color;
            markers.Add(marker);
        }

        private void DrawGraph()
        {
            for (int i = 0; i < dataSets.Count; i++)
            {
                var dataSet = dataSets[i];
                if (!dataSet.IsStatic)
                {
                    // for all sessions that we want to visualize...
                    for (int s = 0; s < Settings.Sessions.Count; s++)
                    {
                        // for all conditions that we want to visualize...
                        for (int c = 0; c < Settings.Conditions.Count; c++)
                        {
                            float colorSaturationOffset = ((Settings.Conditions.Count * s) + c) / (float)(Settings.Conditions.Count * Settings.Sessions.Count);
                            Color.RGBToHSV(dataSet.ObjectColor, out float ColorH, out float ColorS, out float ColorV);
                            if (Settings.Conditions.Count * Settings.Sessions.Count > 3)
                            {
                                ColorS = Math.Max(0.9f, ColorS);
                            }

                            Color objectColor = Color.HSVToRGB(ColorH, ColorS - colorSaturationOffset, ColorV);

                            if (dataSet.ObjectModel != null)
                            {
                                AddMarker(dataSet.ObjectModel, objectColor, dataSet.Id, Settings.Sessions[s], Settings.Conditions[c]);
                            }
                        }
                    }
                }
                else
                {
                    AddMarker(dataSet.ObjectModel, dataSet.ObjectColor, dataSet.Id, -1, -1);
                }
            }
        }

        private void Reset()
        {
            isInitialized = false;
            foreach (var obj in markers)
            {
                if (obj)
                {
                    Destroy(obj);
                }
            }

            markers.Clear();
            dataSets.Clear();
        }

        private void Update()
        {
            if (isInitialized)
            {
                long currentTime = Services.StudyManager().CurrentTimestamp;

                foreach (var markerObject in markers)
                {
                    TrajectoryMarker marker = markerObject.GetComponent<TrajectoryMarker>();

                    var dataSet = Services.DataManager().DataSets[marker.DataSetId];
                    if (dataSet.IsStatic)
                    {
                        continue; // skip static objects
                    }

                    int session = marker.SessionId;
                    int condition = marker.ConditionId;

                    int earlierIndex = dataSet.GetIndexFromTimestamp(currentTime, session, condition);
                    var earlierInfoObject = dataSet.GetInfoObjects(session, condition)[earlierIndex];
                    if (earlierIndex + 1 >= dataSet.GetInfoObjects(session, condition).Count || earlierInfoObject.Timestamp >= currentTime)
                    {
                        marker.transform.localPosition = earlierInfoObject.Position;
                        marker.transform.localRotation = earlierInfoObject.Rotation;
                    }
                    else
                    {
                        int laterIndex = earlierIndex + 1;
                        var laterInfoObject = dataSet.GetInfoObjects(session, condition)[laterIndex];
                        float interpolant = (currentTime - earlierInfoObject.Timestamp) * 1.0f / (laterInfoObject.Timestamp - earlierInfoObject.Timestamp) * 1.0f;
                        marker.transform.localPosition = Vector3.Lerp(earlierInfoObject.Position, laterInfoObject.Position, interpolant);
                        marker.transform.localRotation = Quaternion.Lerp(earlierInfoObject.Rotation, laterInfoObject.Rotation, interpolant);
                    }
                }
            }
        }
    }
}