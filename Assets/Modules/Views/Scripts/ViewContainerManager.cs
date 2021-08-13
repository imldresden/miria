// ------------------------------------------------------------------------------------
// <copyright file="ViewContainerManager.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This Unity component manages all <see cref="ViewContainer">ViewContainers</see> which typically contain one or more 2D visualizations.
    /// This class updates all the containers, takes care of their hierarchy and allows to easily activate or deactivate them.
    /// </summary>
    public class ViewContainerManager : MonoBehaviour
    {
        private List<int> conditions = new List<int>();
        private long currentTimeFilterMax = long.MinValue;
        private long currentTimeFilterMin = long.MaxValue;
        private List<AnalysisObject> dataSets = new List<AnalysisObject>();
        private bool isInitialized = false;
        private long maxTimestamp = long.MinValue;
        private long minTimestamp = long.MaxValue;
        private List<int> sessions = new List<int>();
        private Dictionary<int, List<MediaSource>> mediaSources = new Dictionary<int, List<MediaSource>>();

        /// <summary>
        /// Gets the correct <see cref="MediaSource"/> for a view container, based on the provided session and condition id.
        /// </summary>
        /// <param name="containerId">The id of the view container.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="conditionId">The condition id.</param>
        /// <returns>Returns a valid <see cref="MediaSource"/> if one exists for the <paramref name="containerId"/> or null if none can be found.</returns>
        public MediaSource GetMediaSourceForContainer(int containerId, int sessionId, int conditionId)
        {
            if (mediaSources.ContainsKey(containerId))
            {
                List<MediaSource> list = mediaSources[containerId];
                foreach (var video in list)
                {
                    if ((video.SessionId == -1 || video.SessionId == sessionId) && (video.ConditionId == -1 || video.ConditionId == conditionId))
                    {
                        return video;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Initializes the <see cref="ViewContainerManager"/>.
        /// </summary>
        public void Init()
        {
            if (isInitialized)
            {
                Reset(); // reset if we are already initialized
            }

            if (Services.DataManager() == null || Services.DataManager().CurrentStudy == null || Services.DataManager().DataSets == null)
            {
                // no study loaded
                return; // vis is now reset, we return because there is nothing to load
            }

            // update the media sources, if available
            UpdateMediaSources(Services.DataManager().CurrentStudy.MediaSources);

            // update the list of sessions and conditions
            UpdateSessionFilter(); 

            for (int i = 0; i < Services.DataManager().DataSets.Count; i++)
            {
                dataSets.Add(Services.DataManager().DataSets[i]);
            }

            // get filter min/max
            if (Services.StudyManager() != null)
            {
                // get min and max time stamp
                foreach (int s in sessions)
                {
                    foreach (int c in conditions)
                    {
                        foreach (var dataSet in dataSets)
                        {
                            minTimestamp = Math.Min(dataSet.GetMinTimestamp(s, c), minTimestamp);
                            maxTimestamp = Math.Max(dataSet.GetMaxTimestamp(s, c), maxTimestamp);
                        }
                    }
                }

                // set filtered min and max, based on filter value [0,1] and global min/max
                currentTimeFilterMin = (long)(Services.StudyManager().CurrentTimeFilter.MinTime * (maxTimestamp - minTimestamp)) + minTimestamp;
                currentTimeFilterMax = (long)(Services.StudyManager().CurrentTimeFilter.MaxTime * (maxTimestamp - minTimestamp)) + minTimestamp;
            }

            isInitialized = true;
        }

        /// <summary>
        /// Updates the list of <see cref="MediaSource">MediaSources</see>.
        /// </summary>
        /// <param name="sources">The new media sources.</param>
        public void UpdateMediaSources(List<MediaSource> sources)
        {
            mediaSources.Clear();
            foreach (var source in sources)
            {
                if (mediaSources.ContainsKey(source.AnchorId))
                {
                    mediaSources[source.AnchorId].Add(source);
                }
                else
                {
                    mediaSources.Add(source.AnchorId, new List<MediaSource>());
                    mediaSources[source.AnchorId].Add(source);
                }
            }
        }

        private void LateUpdate()
        {
            if (isInitialized)
            {
                UpdateViewContainers();
            }
        }

        private void OnSessionFilterChange()
        {
            UpdateSessionFilter();
        }

        private void OnStudyChange(int index)
        {
            Init();
        }

        private void OnTimeFilterUpdate(TimeFilter timeFilter)
        {
            // set filtered min and max, based on filter value [0,1] and global min/max
            currentTimeFilterMin = (long)(timeFilter.MinTime * (maxTimestamp - minTimestamp)) + minTimestamp;
            currentTimeFilterMax = (long)(timeFilter.MaxTime * (maxTimestamp - minTimestamp)) + minTimestamp;
        }

        private void Reset()
        {
            dataSets.Clear();

            minTimestamp = long.MaxValue;
            maxTimestamp = long.MinValue;
            isInitialized = false;
        }

        private void Start()
        {
            if (Services.StudyManager())
            {
                Services.StudyManager().SessionFilterEventBroadcast.AddListener(OnSessionFilterChange);
                Services.StudyManager().TimeFilterEventBroadcast.AddListener(OnTimeFilterUpdate);
                Services.StudyManager().StudyChangeBroadcast.AddListener(OnStudyChange);
            }

            Init();
        }

        private void UpdateSessionFilter()
        {
            if (Services.StudyManager())
            {
                sessions = Services.StudyManager().CurrentStudySessions;
                conditions = Services.StudyManager().CurrentStudyConditions;
            }
        }

        private void UpdateViewContainers()
        {
            if (Services.VisManager() == null)
            {
                return;
            }

            var containers = Services.VisManager().ViewContainers;
            long currentTime = Services.StudyManager().CurrentTimestamp;

            foreach (var element in containers)
            {
                var id = element.Key;
                var viewContainer = element.Value;
                if (viewContainer.ParentId != -1)
                {
                    // This view container has a parent and could have changed its transform.
                    // get the AnalysisObject that is this container's parent
                    var parent = dataSets.Find(
                        delegate(AnalysisObject obj)
                        {
                            return obj.Id == viewContainer.ParentId;
                        });

                    // skip this container if the parent is not part of our list of AnalysisObjects
                    if (parent == null)
                    {
                        viewContainer.gameObject.SetActive(false);
                        continue;
                    }

                    // skip this container if more than one session or condition is selected
                    if (sessions.Count != 1 || conditions.Count != 1)
                    {
                        viewContainer.gameObject.SetActive(false);
                        continue;

                        // ToDo: support multiple sessions and conditions. Ideally, automatically create one ViewContainer per session/condition, keep its settings synced to the others and only show data for the current session/condition
                    }
                    else
                    {
                        // (Settings.Sessions.Count == 1 && Settings.Conditions.Count == 1)
                        if (currentTime < currentTimeFilterMin || currentTime > currentTimeFilterMax)
                        {
                            viewContainer.gameObject.SetActive(false);
                            continue;
                        }

                        viewContainer.gameObject.SetActive(true);

                        // get parent position and orientation at current time stamp
                        int session = sessions[0];
                        int condition = conditions[0];
                        Vector3 parentPosition;
                        Quaternion parentOrientation;

                        int earlierIndex = parent.GetIndexFromTimestamp(currentTime, session, condition);
                        var earlierSample = parent.GetInfoObjects(session, condition)[earlierIndex];
                        if (earlierIndex + 1 >= parent.GetInfoObjects(session, condition).Count || earlierSample.Timestamp >= currentTime)
                        {
                            parentPosition = earlierSample.Position;
                            parentOrientation = earlierSample.Rotation;
                        }
                        else
                        {
                            int laterIndex = earlierIndex + 1;
                            var laterSample = parent.GetInfoObjects(session, condition)[laterIndex];
                            float interpolant = (currentTime - earlierSample.Timestamp) * 1.0f / (laterSample.Timestamp - earlierSample.Timestamp) * 1.0f;
                            parentPosition = Vector3.Lerp(earlierSample.Position, laterSample.Position, interpolant);
                            parentOrientation = Quaternion.Lerp(earlierSample.Rotation, laterSample.Rotation, interpolant);
                        }

                        // update child container transform accordingly
                        Quaternion newRotation = parentOrientation;
                        Vector3 newPosition = parentPosition + (newRotation * viewContainer.PositionOffset);
                        if (newRotation != viewContainer.transform.localRotation || newPosition != viewContainer.transform.localPosition)
                        {
                            viewContainer.UpdateTransform(newPosition, newRotation);
                            ////viewContainer.transform.localRotation = newRotation;
                            ////viewContainer.transform.localPosition = newPosition;
                            ////viewContainer.transform.hasChanged = true;
                        }
                    }
                }
            }
        }
    }
}