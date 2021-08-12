// ------------------------------------------------------------------------------------
// <copyright file="Vis2DLocation.cs" company="Technische Universität Dresden">
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
    /// This Unity component is a 2D location visualization. It implements <see cref="AbstractView"/> and <see cref="IConfigurableVisualization"/>.
    /// </summary>
    public class Vis2DLocation : AbstractView, IConfigurableVisualization
    {
        /// <summary>
        /// The prefab for the location markers.
        /// </summary>
        public GameObject MarkerPrefab;

        /// <summary>
        /// The prefab for the settings view.
        /// </summary>
        public AbstractSettingsView SettingsViewPrefab;

        /// <summary>
        /// The duration in seconds after which a location marker is hidden when not updated.
        /// </summary>
        public float TimeWindow;

        /// <summary>
        /// A list of the current analysis objects.
        /// </summary>
        protected List<AnalysisObject> dataSets = new List<AnalysisObject>();

        private bool isInitialized = false;
        private readonly List<GameObject> markers = new List<GameObject>();
        private readonly float markerSize = 0.05f;
        private Transform visAnchorTransform;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => false;

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.Location2D;

        /// <summary>
        /// Initializes the visualization with the provided settings.
        /// </summary>
        /// <param name="settings">The settings to use for this visualization.</param>
        public override void Init(VisProperties settings)
        {
            if (isInitialized)
            {
                Reset();
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
                dataSets.Add((AnalysisObject)Services.DataManager().DataSets[dataSetIndex]);
            }

            DrawGraph(); // Basically, this just adds markers

            isInitialized = true;
        }

        /// <summary>
        /// Opens the settings user interface for the visualization.
        /// </summary>
        public void OpenSettingsUI()
        {
            AbstractSettingsView settingsView = Instantiate(SettingsViewPrefab);
            settingsView.Init(this, false, false);
            settingsView.gameObject.SetActive(true);
        }

        /// <summary>
        /// Updates the view.
        /// </summary>
        /// <remarks>This currently does nothing.</remarks>
        public override void UpdateView()
        {
            UpdateView(Settings);
        }

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public override void UpdateView(VisProperties settings)
        {
            Init(settings);
        }

        private void AddMarker(Color color, float size, int dataSetId, int sessionId, int conditionId)
        {
            GameObject marker = Instantiate(MarkerPrefab, Anchor);
            Renderer renderer = marker.GetComponent<Renderer>();
            TrajectoryMarker markerData = marker.GetComponent<TrajectoryMarker>();
            markerData.DataSetId = dataSetId;
            markerData.SessionId = sessionId;
            markerData.ConditionId = conditionId;
            if (visAnchorTransform)
            {
                marker.transform.localScale = new Vector3(size / visAnchorTransform.localScale.x, size / visAnchorTransform.localScale.y, 1);
            }
            else
            {
                marker.transform.localScale = new Vector3(size, size, 1);
            }

            renderer.material.color = color;
            marker.GetComponent<Renderer>().enabled = false; // do not show initially
            markers.Add(marker);
        }

        private void DrawGraph()
        {
            foreach (var dataSet in dataSets)
            {
                if (dataSet.IsStatic)
                {
                    continue; // static datasets don't get shown
                }

                // for all sessions that we want to visualize
                for (int s = 0; s < Settings.Sessions.Count; s++)
                {
                    // for all conditions that we want to visualize
                    for (int c = 0; c < Settings.Conditions.Count; c++)
                    {
                        float colorSaturationOffset = ((Settings.Conditions.Count * s) + c) / (float)(Settings.Conditions.Count * Settings.Sessions.Count);
                        Color.RGBToHSV(dataSet.ObjectColor, out float colorH, out float colorS, out float colorV);
                        if (Settings.Conditions.Count * Settings.Sessions.Count > 3)
                        {
                            colorS = Math.Max(0.9f, colorS);
                        }

                        Color objectColor = Color.HSVToRGB(colorH, colorS - colorSaturationOffset, colorV);
                        AddMarker(objectColor, markerSize, dataSet.Id, Settings.Sessions[s], Settings.Conditions[c]); // add a visual marker game object
                    }
                }
            }
        }

        private Vector3 ProjectToAnchorPlane(Vector3 rawPosition, Transform worldAnchor, Transform transform)
        {
            Vector3 position = worldAnchor.TransformPoint(rawPosition);
            Vector3 projectedVector = position - Vector3.Project(position - transform.position, transform.forward.normalized);
            return projectedVector;
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

            if (dataSets != null)
            {
                dataSets.Clear();
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (isInitialized)
            {
                Transform worldAnchor;
                if (Services.VisManager() != null)
                {
                    worldAnchor = Services.VisManager().VisAnchor;
                    if (Services.VisManager().ViewContainers.ContainsKey(Settings.AnchorId))
                    {
                        visAnchorTransform = Services.VisManager().ViewContainers[Settings.AnchorId].transform;
                    }
                    else
                    {
                        visAnchorTransform = this.transform; // this does not really help us at all, but at least we won't crash. :>
                    }
                }
                else
                {
                    worldAnchor = this.transform;
                    visAnchorTransform = this.transform;
                }

                long currentTime = Services.StudyManager().CurrentTimestamp;

                foreach (var markerObject in markers)
                {
                    TrajectoryMarker marker = markerObject.GetComponent<TrajectoryMarker>();

                    var dataSet = Services.DataManager().DataSets[marker.DataSetId];
                    if (dataSet.IsStatic)
                    {
                        continue;
                    }

                    int session = marker.SessionId;
                    int condition = marker.ConditionId;

                    int currentIndex = dataSet.GetIndexFromTimestamp(currentTime, session, condition);
                    var currentInfoObject = dataSet.GetInfoObjects(session, condition)[currentIndex];

                    if (currentInfoObject.Timestamp > currentTime)
                    {
                        Debug.LogWarning("DataSet.GetValueAtTime returned timestamp larger than argument: " + currentInfoObject.Timestamp + " > " + currentTime);
                        if (currentIndex > 0)
                        {
                            currentInfoObject = dataSet.GetInfoObjects(session, condition)[currentIndex - 1]; // this should never happen but let's get the previous timestamp
                        }
                    }

                    if (currentTime > currentInfoObject.Timestamp + (TimeWindow * TimeSpan.TicksPerSecond))
                    {
                        // ToDo: Fade out?
                        marker.GetComponent<Renderer>().enabled = false;
                    }
                    else
                    {
                        marker.GetComponent<Renderer>().enabled = true;
                        marker.transform.position = ProjectToAnchorPlane(currentInfoObject.Position, worldAnchor, visAnchorTransform);
                        if (visAnchorTransform)
                        {
                            marker.transform.localScale = new Vector3(currentInfoObject.Scale.x * markerSize / visAnchorTransform.localScale.x, currentInfoObject.Scale.y * markerSize / visAnchorTransform.localScale.y, 1);
                        }
                        else
                        {
                            marker.transform.localScale = new Vector3(currentInfoObject.Scale.x * markerSize, currentInfoObject.Scale.y * markerSize, 1);
                        }
                    }
                }
            }
        }
    }
}