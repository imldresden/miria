// -------------------------------------------------------------------------------------
// <copyright file="Vis2DScatterplotShader.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// -------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using IMLD.MixedRealityAnalysis.Core;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a 2D scatter plot. It implements <see cref="AbstractView"/> and <see cref="IConfigurableVisualization"/>.
    /// </summary>
    public class Vis2DScatterplotShader : AbstractView, IConfigurableVisualization
    {
        /// <summary>
        /// The prefab for a scatter dot.
        /// </summary>
        public DotShaderMarker MarkerPrefab;

        /// <summary>
        /// The prefab for the settings view.
        /// </summary>
        public AbstractSettingsView SettingsViewPrefab;

        private long currentTimeFilterMin = long.MaxValue;
        private readonly List<AnalysisObject> dataSets = new List<AnalysisObject>();
        private bool isInitialized = false;
        private bool isRedrawNecessary = true;
        private long lastRenderedTimestamp;
        private readonly List<DotShaderMarker> markers = new List<DotShaderMarker>();
        private long maxTimestamp = long.MinValue;
        private long minTimestamp = long.MaxValue;
        private Transform visAnchorTransform;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => false;

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.Scatterplot2D;

        /// <summary>
        /// Distance based filtering of samples.
        /// This is used as a <see cref="Func{Sample, Sample, bool}"/> to filter out consecutive samples that are very close to each other.
        /// </summary>
        /// <param name="currentSample">The current sample that might get filtered out.</param>
        /// <param name="previousSample">The previous sample.</param>
        /// <returns><see langword="true"/> if the current sample should be included, <see langword="false"/> if it should be discarded.</returns>
        public static bool DistanceFilter(Sample currentSample, Sample previousSample)
        {
            float distance = (currentSample.Position - previousSample.Position).magnitude;
            if (distance <= 0.01)
            {
                return false;
            }

            return true;
        }

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

            // get filter min/max and max number of samples
            if (Services.StudyManager() != null)
            {
                // get min and max timestamp & generate markers
                foreach (int s in Settings.Sessions)
                {
                    foreach (int c in Settings.Conditions)
                    {
                        foreach (var dataSet in dataSets)
                        {
                            minTimestamp = Math.Min(dataSet.GetMinTimestamp(s, c), minTimestamp);
                            maxTimestamp = Math.Max(dataSet.GetMaxTimestamp(s, c), maxTimestamp);
                            var marker = Instantiate<DotShaderMarker>(MarkerPrefab, transform);
                            marker.Condition = c;
                            marker.Session = s;
                            marker.ObjectId = dataSet.Id;
                            marker.MeshFilter.sharedMesh = new Mesh();
                            marker.Material = new Material(marker.Material);
                            markers.Add(marker);
                        }
                    }
                }

                // set filtered min and max, based on filter value [0,1] and global min/max
                currentTimeFilterMin = (long)(Services.StudyManager().CurrentTimeFilter.MinTime * (maxTimestamp - minTimestamp)) + minTimestamp;
                lastRenderedTimestamp = currentTimeFilterMin;

                Services.StudyManager().TimeFilterEventBroadcast.AddListener(TimeFilterUpdated); // set listener so we can get notified about future updates
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

        private void DrawGraph()
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

            foreach (var dataSet in dataSets)
            {
                if (dataSet.IsStatic)
                {
                    continue; // static datasets don't get shown
                }

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
                        objectColor.a = 0.9f;

                        // find first sample & last sample id
                        var firstSample = dataSet.GetIndexFromTimestamp(currentTimeFilterMin, Settings.Sessions[s], Settings.Conditions[c]);
                        var lastSample = dataSet.GetIndexFromTimestamp(Services.StudyManager().CurrentTimestamp, Settings.Sessions[s], Settings.Conditions[c]);

                        List<Vector3> vertexList = new List<Vector3>();
                        List<int> indexList = new List<int>();
                        List<Color> colorList = new List<Color>();

                        // draw all samples between first and last
                        int i = 0;
                        foreach (var sample in dataSet.GetFilteredInfoObjects(Settings.Sessions[s], Settings.Conditions[c], firstSample, lastSample, DistanceFilter))
                        {
                            vertexList.Add(ProjectToAnchorPlane(sample.Position, worldAnchor, visAnchorTransform));
                            indexList.Add(i);
                            colorList.Add(objectColor);
                            i++;
                        }

                        Vector3[] vertices = vertexList.ToArray();
                        int[] indices = indexList.ToArray();
                        Color[] colors = colorList.ToArray();

                        int n = 0;
                        foreach (var marker in markers)
                        {
                            if (marker.ObjectId == dataSet.Id && marker.Session == Settings.Sessions[s] && marker.Condition == Settings.Conditions[c])
                            {
                                marker.MeshFilter.sharedMesh.Clear();
                                marker.Material.SetVector("_Scale", visAnchorTransform.localScale);
                                marker.Material.SetMatrix("_WorldToLocalMatrix", visAnchorTransform.worldToLocalMatrix);
                                marker.Material.SetColor("_Color", objectColor);
                                marker.MeshFilter.sharedMesh.vertices = vertices;
                                marker.MeshFilter.sharedMesh.colors = colors;
                                marker.MeshFilter.sharedMesh.SetIndices(indices, MeshTopology.Points, 0);
                                marker.MeshFilter.sharedMesh.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
                                marker.Renderer.sharedMaterial = marker.Material;
                                marker.Renderer.sharedMaterial.renderQueue += n++;
                                break;
                            }
                        }
                    }
                }
            }

            // update last timestamp
            lastRenderedTimestamp = Services.StudyManager().CurrentTimestamp;
            isRedrawNecessary = false;
        }

        private Vector3 ProjectToAnchorPlane(Vector3 rawPosition, Transform worldAnchor, Transform transform)
        {
            Vector3 position = worldAnchor.TransformPoint(rawPosition);
            Vector3 projectedVector = transform.position + Vector3.ProjectOnPlane(position - transform.position, transform.forward.normalized);
            return projectedVector;
        }

        private void Reset()
        {
            if (markers != null && markers.Count > 0)
            {
                foreach (var marker in markers)
                {
                    if (marker.MeshFilter)
                    {
                        DestroyImmediate(marker.MeshFilter.sharedMesh);
                    }

                    if (marker.Material)
                    {
                        DestroyImmediate(marker.Material);
                    }

                    DestroyImmediate(marker);
                }

                markers.Clear();
            }

            if (dataSets != null)
            {
                dataSets.Clear();
            }
        }

        private void TimeFilterUpdated(TimeFilter timeFilter)
        {
            // set filtered min and max, based on filter value [0,1] and global min/max
            currentTimeFilterMin = (long)(timeFilter.MinTime * (maxTimestamp - minTimestamp)) + minTimestamp;
            isRedrawNecessary = true;
        }

        // Update is called once per frame
        private void Update()
        {
            if (isInitialized)
            {
                long currentTime = Services.StudyManager().CurrentTimestamp;

                if (isRedrawNecessary)
                {
                    lastRenderedTimestamp = currentTimeFilterMin;
                    DrawGraph();
                }
                else if (currentTime < lastRenderedTimestamp)
                {
                    lastRenderedTimestamp = currentTimeFilterMin;
                    DrawGraph();
                }
                else if (currentTime > lastRenderedTimestamp)
                {
                    DrawGraph();
                }
            }
        }
    }
}