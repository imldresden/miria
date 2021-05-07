// ------------------------------------------------------------------------------------
// <copyright file="Vis3DTrails.cs" company="Technische Universität Dresden">
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
using IMLD.MixedRealityAnalysis.Utils;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component visualizes 3D trails. It implements <see cref="AbstractView"/> and <see cref="IConfigurableVisualization"/>.
    /// </summary>
    public class Vis3DTrails : AbstractView, IConfigurableVisualization
    {
        /// <summary>
        /// The prefab for a line in this visualization.
        /// </summary>
        public GameObject LinePrefab;

        /// <summary>
        /// The prefab for the settings view.
        /// </summary>
        public AbstractSettingsView SettingsViewPrefab;

        /// <summary>
        /// A list of the current analysis objects.
        /// </summary>
        protected List<AnalysisObject> dataSets = new List<AnalysisObject>();

        private long currentTimeFilterMax = long.MinValue;
        private long currentTimeFilterMin = long.MaxValue;
        private bool isInitialized = false;
        private long maxTimestamp = long.MinValue;
        private long minTimestamp = long.MaxValue;
        private readonly List<CustomTubeRenderer> primitives = new List<CustomTubeRenderer>();
        private readonly float trailTime = 2.0f;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => true;

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.Trail3D;

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

            // get filter min/max
            if (Services.StudyManager() != null)
            {
                minTimestamp = Services.StudyManager().MinTimestamp;
                maxTimestamp = Services.StudyManager().MaxTimestamp;
                currentTimeFilterMin = Services.StudyManager().CurrentTimeFilter.MinTimestamp;
                currentTimeFilterMax = Services.StudyManager().CurrentTimeFilter.MaxTimestamp;
                Services.StudyManager().TimeFilterEventBroadcast.AddListener(TimeFilterUpdated); // set listener so we can get notified about future updates
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
            settingsView.Init(this, false, true);
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

        /// <summary>
        /// Takes the provided properties and sets defaults where necessary.
        /// </summary>
        /// <param name="properties">The properties for the view.</param>
        /// <returns>The modified <see cref="VisProperties"/>.</returns>
        protected override VisProperties ParseSettings(VisProperties properties)
        {
            properties = base.ParseSettings(properties);
            if (!properties.TryGet("useSpeed", out List<bool> useSpeedList) || useSpeedList.Count != properties.ObjectIds.Count)
            {
                useSpeedList = new List<bool>();
                for (int i = 0; i < properties.ObjectIds.Count; i++)
                {
                    useSpeedList.Add(false);
                }

                properties.Set("useSpeed", useSpeedList);
            }

            return properties;
        }

        //private bool CheckData(Vector3 position, ref Vector3 previousSamplePosition, float minDistance)
        //{
        //    // first sample
        //    if (previousSamplePosition.sqrMagnitude == 0f)
        //    {
        //        previousSamplePosition = position;
        //        return true;
        //    }
        //    else
        //    {
        //        float distance = (position - previousSamplePosition).magnitude;
        //        if (distance >= minDistance)
        //        {
        //            previousSamplePosition = position;
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        private bool CheckOutlier(long currentTimestamp, Vector3 currentPosition, ref long previousSampleTime, ref Vector3 previousSamplePosition, float maxSpeed)
        {
            // first sample
            if (previousSampleTime == 0 || previousSamplePosition.sqrMagnitude == 0f)
            {
                return false;
            }
            else
            {
                float diffSeconds = (float)(currentTimestamp - previousSampleTime) / TimeSpan.TicksPerSecond;
                float distance = (currentPosition - previousSamplePosition).magnitude;
                if (distance / diffSeconds > maxSpeed)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckReductionFilter(long timestamp, Vector3 position, ref long previousSampleTime, ref Vector3 previousSamplePosition, int fps, float minDistance)
        {
            // first sample
            if (previousSampleTime == 0 || previousSamplePosition.sqrMagnitude == 0f)
            {
                previousSampleTime = timestamp;
                previousSamplePosition = position;
                return true;
            }
            else
            {
                int diffMilliSeconds = (int)((timestamp - previousSampleTime) / 10000);
                float distance = (position - previousSamplePosition).magnitude;
                if (diffMilliSeconds >= 1000 / fps && distance >= minDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawGraph()
        {
            for (int i = 0; i < dataSets.Count; i++)
            {
                var dataSet = dataSets[i];

                // static datasets don't get trajectories
                if (!dataSet.IsStatic)
                {
                    // check & decide if we should encode speed to the trajectory
                    bool useSpeed;
                    if (Settings.TryGet("useSpeed", out List<bool> useSpeedList))
                    {
                        if (useSpeedList != null && useSpeedList.Count == dataSets.Count && useSpeedList[i] == true)
                        {
                            useSpeed = true;
                        }
                        else
                        {
                            useSpeed = false;
                        }
                    }
                    else
                    {
                        useSpeed = false;
                    }

                    DrawTrajectory(dataSet, useSpeed);
                }
            }
        }

        private void DrawTrajectory(AnalysisObject dataSet, bool useSpeed)
        {
            // for all sessions that we want to visualize...
            for (int s = 0; s < Settings.Sessions.Count; s++)
            {
                // for all conditions that we want to visualize...
                for (int c = 0; c < Settings.Conditions.Count; c++)
                {
                    var line = Instantiate(LinePrefab, Anchor);
                    var lineComponent = line.GetComponent<CustomTubeRenderer>();
                    lineComponent.startWidth = 0.0f;
                    lineComponent.endWidth = 0.006f;
                    float colorSaturationOffset = ((Settings.Conditions.Count * s) + c) / (float)(Settings.Conditions.Count * Settings.Sessions.Count);

                    UpdateSegments(dataSet, Settings.Sessions[s], Settings.Conditions[c], useSpeed, colorSaturationOffset, out List<List<Vector3>> segments, out List<List<Color>> colorSegments);

                    lineComponent.SetPositions(segments, colorSegments);
                    primitives.Add(lineComponent);
                }
            }
        }

        private Color MapSpeedToColor(float currentSpeed)
        {
            return ColormapViridis.GetValueAt(currentSpeed / 2.0f);
        }

        private void RedrawGraph()
        {
            int primitiveCounter = 0;
            for (int i = 0; i < dataSets.Count; i++)
            {
                var dataSet = dataSets[i];

                // static datasets don't get trajectories
                if (!dataSet.IsStatic)
                {
                    // for all sessions that we want to visualize...
                    for (int s = 0; s < Settings.Sessions.Count; s++)
                    {
                        // for all conditions that we want to visualize...
                        for (int c = 0; c < Settings.Conditions.Count; c++)
                        {
                            bool useSpeed;
                            if (Settings.TryGet("useSpeed", out List<bool> useSpeedList))
                            {
                                if (useSpeedList != null && useSpeedList.Count == dataSets.Count && useSpeedList[i] == true)
                                {
                                    useSpeed = true;
                                }
                                else
                                {
                                    useSpeed = false;
                                }
                            }
                            else
                            {
                                useSpeed = false;
                            }

                            float colorSaturationOffset = ((Settings.Conditions.Count * s) + c) / (float)(Settings.Conditions.Count * Settings.Sessions.Count);
                            UpdateSegments(dataSet, Settings.Sessions[s], Settings.Conditions[c], useSpeed, colorSaturationOffset, out List<List<Vector3>> segments, out List<List<Color>> colorSegments);

                            primitives[primitiveCounter].SetPositions(segments, colorSegments);
                            primitiveCounter++;
                        }
                    }
                }
            }
        }

        private void Reset()
        {
            isInitialized = false;
            foreach (var obj in primitives)
            {
                if (obj)
                {
                    Destroy(obj.gameObject);
                }
            }

            primitives.Clear();

            dataSets.Clear();

            minTimestamp = long.MaxValue;
            maxTimestamp = long.MinValue;
        }

        private void TimeFilterUpdated(TimeFilter timeFilter)
        {
            // set filtered min and max, based on filter value [0,1] and global min/max
            currentTimeFilterMin = (long)(timeFilter.MinTime * (maxTimestamp - minTimestamp)) + minTimestamp;
            currentTimeFilterMax = (long)(timeFilter.MaxTime * (maxTimestamp - minTimestamp)) + minTimestamp;
        }

        private void Update()
        {
            if (isInitialized)
            {
                RedrawGraph();
            }
        }

        private void UpdateSegments(AnalysisObject dataSet, int session, int condition, bool useSpeed, float colorSaturationOffset, out List<List<Vector3>> segments, out List<List<Color>> colorSegments)
        {
            Color.RGBToHSV(dataSet.ObjectColor, out float colorH, out float colorS, out float colorV);
            if (Settings.Conditions.Count * Settings.Sessions.Count > 3)
            {
                colorS = Math.Max(0.9f, colorS);
            }

            Color objectColor = Color.HSVToRGB(colorH, colorS - colorSaturationOffset, colorV);

            var infoObjects = dataSet.GetInfoObjects(session, condition); // get data for current session/condition
            segments = new List<List<Vector3>>();
            List<Vector3> points = new List<Vector3>();
            colorSegments = new List<List<Color>>();
            List<Color> colors = new List<Color>();

            Vector3 previousPosition = new Vector3(0, 0, 0);
            long previousTimestamp = 0;
            Vector3 currentPosition;
            long currentTime = Services.StudyManager().CurrentTimestamp;
            int currentIndex = dataSet.GetIndexFromTimestamp(currentTime, session, condition);
            int firstIndex = dataSet.GetIndexFromTimestamp(currentTime - (long)(trailTime * TimeSpan.TicksPerSecond), session, condition);
            int alphaDropOffIndex = dataSet.GetIndexFromTimestamp(currentTime - (long)(0.75 * trailTime * TimeSpan.TicksPerSecond), session, condition);
            for (int i = firstIndex; i < currentIndex; i++)
            {
                Sample o = infoObjects[i];

                if (float.IsNaN(o.Position.x) || float.IsNaN(o.Position.y) || float.IsNaN(o.Position.z))
                {
                    throw new ArgumentException("float.NaN is not a valid position.");
                }

                currentPosition = o.Position;

                // check data to decide if we want to add this measurement
                if (!CheckReductionFilter(o.Timestamp, currentPosition, ref previousTimestamp, ref previousPosition, 15, 0.03f))
                {
                    continue;
                }

                // checks our time filter
                if (o.Timestamp < currentTimeFilterMin || o.Timestamp > currentTimeFilterMax)
                {
                    continue;
                }

                // check if probably an outlier (too fast)
                if (true || !CheckOutlier(o.Timestamp, currentPosition, ref previousTimestamp, ref previousPosition, 10.0f))
                {
                    points.Add(currentPosition); // add current point

                    // compute color based on speed
                    float diffSeconds = (float)(o.Timestamp - previousTimestamp) / TimeSpan.TicksPerSecond;
                    float diffDistance = (currentPosition - previousPosition).magnitude;
                    float currentSpeed = diffDistance / diffSeconds;
                    Color color;
                    if (useSpeed)
                    {
                        color = MapSpeedToColor(currentSpeed);
                    }
                    else
                    {
                        color = objectColor;
                    }

                    // set alpha
                    if (alphaDropOffIndex > firstIndex)
                    {
                        color.a = (i - firstIndex) / ((alphaDropOffIndex - firstIndex) * 1.0f);
                    }

                    colors.Add(color);
                }
                else
                {
                    if (points.Count > 3)
                    {
                        segments.Add(points);
                        colors[0] = colors[1]; // first speed value cannot be correct, correct color
                        colorSegments.Add(colors);
                    }

                    points = new List<Vector3>();
                    points.Add(currentPosition);

                    colors = new List<Color>();

                    // compute color based on speed
                    float diffSeconds = (float)(o.Timestamp - previousTimestamp) / TimeSpan.TicksPerSecond;
                    float diffDistance = (currentPosition - previousPosition).magnitude;
                    float currentSpeed = diffDistance / diffSeconds;
                    Color color;
                    if (useSpeed)
                    {
                        color = MapSpeedToColor(currentSpeed);
                    }
                    else
                    {
                        color = objectColor;
                    }

                    // set alpha
                    if (alphaDropOffIndex > firstIndex)
                    {
                        color.a = (i - firstIndex) / ((alphaDropOffIndex - firstIndex) * 1.0f);
                    }

                    colors.Add(color);
                }

                previousPosition = currentPosition;
                previousTimestamp = o.Timestamp;
            }

            if (points.Count > 3)
            {
                segments.Add(points);
                colors[0] = colors[1]; // first speed value cannot be correct, correct color
                colorSegments.Add(colors);
            }
        }
    }
}