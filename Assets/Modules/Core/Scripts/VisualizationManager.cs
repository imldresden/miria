// ------------------------------------------------------------------------------------
// <copyright file="VisualizationManager.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Network.Messages;
using IMLD.MixedRealityAnalysis.Views;
using UnityEngine;
using UnityEngine.Events;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This unity component manages creation, deletion, and updating visualizations.
    /// </summary>
    public class VisualizationManager : MonoBehaviour
    {
        /// <summary>
        /// The prefab for a 3D coordinate system.
        /// </summary>
        public Vis3DCoordinateSystem CoordinateSystemPrefab;

        /// <summary>
        /// The prefab for the timeline control panel.
        /// </summary>
        public ViewTimelineControl TimelineControlPrefab;

        /// <summary>
        /// The prefab for a view container.
        /// </summary>
        public ViewContainer VisPlaceholderPrefab;

        /// <summary>
        /// The list of prefabs for the generation of visualizations.
        /// </summary>
        public List<AbstractView> VisualizationPrefabs;

        private Vis3DCoordinateSystem coordinateSystemVis;
        private ViewTimelineControl timelineController;

        /// <summary>
        /// Gets the event that is raised when centering the data.
        /// </summary>
        public DataCenteringEvent DataCenteringEventBroadcast { get; } = new DataCenteringEvent();

        /// <summary>
        /// Gets a dictionary of all view containers.
        /// </summary>
        public Dictionary<int, ViewContainer> ViewContainers { get; } = new Dictionary<int, ViewContainer>();

        /// <summary>
        /// Gets the event that is raised when a visualization has been created.
        /// </summary>
        public VisualizationCreatedEvent VisualizationCreatedEventBroadcast { get; } = new VisualizationCreatedEvent();

        /// <summary>
        /// Gets the event that is raised when a visualization has been deleted.
        /// </summary>
        public VisualizationDeletedEvent VisualizationDeletedEventBroadcast { get; } = new VisualizationDeletedEvent();

        /// <summary>
        /// Gets a dictionary of all visualizations.
        /// </summary>
        public Dictionary<Guid, AbstractView> Visualizations { get; } = new Dictionary<Guid, AbstractView>();

        /// <summary>
        /// Centers or un-centers the data to the origin, based on the average position of the samples.
        /// </summary>
        /// <param name="isCentering">Indicated whether to center or to reverse.</param>
        /// <param name="syncWithRemote">Indicates whether the centering should also happen on remote clients.</param>
        public void CenterData(bool isCentering, bool syncWithRemote = true)
        {
            var GlobalOffset = GameObject.FindGameObjectWithTag("GlobalOffset");

            if(GlobalOffset != null)
            {
                // center or un-center data
                if (isCentering == true && GlobalOffset.transform.position == Vector3.zero)
                {
                    Vector3 averagePosition = Vector3.zero;
                    foreach (var studyObject in Services.DataManager().DataSets.Values)
                    {
                        averagePosition += studyObject.AveragePosition;
                    }

                    averagePosition /= Services.DataManager().DataSets.Values.Count;

                    GameObject.FindGameObjectWithTag("GlobalOffset").transform.localPosition -= averagePosition;

                    // send message to the other clients
                    if (syncWithRemote)
                    {
                        var message = new MessageCenterData(isCentering);
                        Services.NetworkManager().SendMessage(message.Pack());
                    }

                    // send notification event that the data was centered or un-centered
                    DataCenteringEventBroadcast.Invoke(true);
                }
                else
                {
                    GameObject.FindGameObjectWithTag("GlobalOffset").transform.localPosition = Vector3.zero;

                    // send message to the other clients
                    if (syncWithRemote)
                    {
                        var message = new MessageCenterData(isCentering);
                        Services.NetworkManager().SendMessage(message.Pack());
                    }

                    // send notification event that the data was centered or un-centered
                    DataCenteringEventBroadcast.Invoke(false);
                }

            }
            
        }

        /// <summary>
        /// Creates a new timeline control.
        /// </summary>
        /// <param name="settings">The settings for the new timeline control.</param>
        /// <returns>A <see cref="GameObject"/> with the timeline control.</returns>
        public GameObject CreateTimelineControl(VisProperties settings)
        {
            settings.VisId = Guid.Empty;
            settings.VisType = VisType.TimelineControl;
            GameObject vis;
            var timelineControl = Instantiate(TimelineControlPrefab);
            timelineControl.Init(settings);
            vis = timelineControl.gameObject;
            timelineController = timelineControl;
            Transform worldAnchor = GameObject.FindGameObjectWithTag("RootWorldAnchor").transform;
            vis.transform.SetParent(worldAnchor, false);
            return vis;
        }

        /// <summary>
        /// Creates a new <see cref="ViewContainer"/> and adds it to the list of containers.
        /// </summary>
        /// <param name="container">The <see cref="VisContainer"/> object representing the settings for the new <see cref="ViewContainer"/>.</param>
        /// <param name="syncWithRemote">Indicates whether the container should also be created on remote clients.</param>
        public void CreateViewContainer(VisContainer container, bool syncWithRemote = true)
        {
            // add to list of containers or update list
            if (ViewContainers.ContainsKey(container.Id))
            {
                // already in list, update
                ViewContainers[container.Id].Init(container);
            }
            else
            {
                // not in list, create and add
                Transform worldAnchor = GameObject.FindGameObjectWithTag("VisRootAnchor").transform;
                var placeholder = Instantiate(VisPlaceholderPrefab, worldAnchor); // instantiate placeholder prefab, set the World Anchor as parent, to make sure every client sees the same
                placeholder.Init(container);
                ViewContainers.Add(container.Id, placeholder.GetComponent<ViewContainer>()); // add to list
            }

            if (syncWithRemote)
            {
                var message = new MessageCreateVisContainer(container);
                Services.NetworkManager().SendMessage(message.Pack());
            }
        }

        /// <summary>
        /// Creates a visualization from a <see cref="VisProperties"/>.
        /// </summary>
        /// <param name="settings">The struct containing the settings for the visualization.</param>
        /// <param name="syncWithRemote">Indicates whether the visualization should also be created on other clients.</param>
        /// <returns>A <see cref="GameObject"/> with the visualization.</returns>
        public GameObject CreateVisualization(VisProperties settings, bool syncWithRemote = true)
        {
            try
            {
                if (Visualizations.ContainsKey(settings.VisId))
                {
                    throw new Exception("VisId " + settings.VisId + " has been used twice!");
                }

                // if the vis id is empty, create a new, non-colliding GUID
                if (settings.VisId == Guid.Empty)
                {
                    settings.VisId = Guid.NewGuid();

                    // This is extremely unlikely to ever happen
                    while (Visualizations.ContainsKey(settings.VisId))
                    {
                        Debug.LogWarning("GUID collision! Probably someone made a mistake. If not, you should start playing the lottery!");
                        settings.VisId = Guid.NewGuid();
                    }
                }

                // special case: timeline control view & coordinate system
                GameObject vis;
                if (settings.VisType == VisType.TimelineControl)
                {
                    var timelineControl = Instantiate(TimelineControlPrefab);
                    timelineControl.Init(settings);
                    vis = timelineControl.gameObject;
                    timelineController = timelineControl;
                }
                else if (settings.VisType == VisType.CoordinateSystem3D)
                {
                    var coordinateSystem = Instantiate(CoordinateSystemPrefab);
                    coordinateSystem.Init(settings);
                    vis = coordinateSystem.gameObject;
                    coordinateSystemVis = coordinateSystem;
                }

                // get correct prefab
                AbstractView visPrefab = null;
                foreach (var prefab in VisualizationPrefabs)
                {
                    if (prefab.VisType == settings.VisType)
                    {
                        visPrefab = prefab;
                        break;
                    }
                }

                if (visPrefab == null)
                {
                    throw new Exception("Attempt to spawn unknown visualization! Type: " + settings.VisType);
                }

                // instantiate prefab
                var visInstance = Instantiate(visPrefab);
                visInstance.Init(settings);
                vis = visInstance.gameObject;

                if (settings.AnchorId != -1 && ViewContainers.ContainsKey(settings.AnchorId))
                {
                    // anchor is available
                    ViewContainers[settings.AnchorId].AttachVis(vis.GetComponent<AbstractView>());
                }
                else if (settings.VisType == VisType.TimelineControl)
                {
                    // anchor not available, put at world anchor
                    Transform worldAnchor = GameObject.FindGameObjectWithTag("RootWorldAnchor").transform;
                    vis.transform.SetParent(worldAnchor, false);
                }
                else
                {
                    // anchor not available, put at world anchor
                    Transform worldAnchor = GameObject.FindGameObjectWithTag("VisRootAnchor").transform;
                    vis.transform.SetParent(worldAnchor, false);
                }

                Visualizations[settings.VisId] = vis.GetComponent<AbstractView>();

                // at this point, creation was successful; send message to the other clients if needed
                if (syncWithRemote)
                {
                    var message = new MessageCreateVisualization(settings);
                    Services.NetworkManager().SendMessage(message.Pack());
                }

                // send notification event that a new vis was created
                VisualizationCreatedEventBroadcast.Invoke(settings);

                return vis;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Creation of requested Visualization failed.");
                Debug.LogError(e.Message);
                Debug.LogError(e.StackTrace);
                return new GameObject("Creation Failed");
            }
        }

        /// <summary>
        /// Deletes all existing <see cref="ViewContainer">ViewContainers</see>.
        /// </summary>
        /// <param name="syncWithRemote">Indicates whether the containers should also be deleted on remote clients.</param>
        public void DeleteAllViewContainers(bool syncWithRemote = true)
        {
            if (ViewContainers != null)
            {
                foreach (var container in ViewContainers)
                {
                    Destroy(container.Value.gameObject);
                }

                ViewContainers.Clear();
            }

            if (syncWithRemote)
            {
                var message = new MessageDeleteAllVisContainers();
                Services.NetworkManager().SendMessage(message.Pack());
            }
        }

        /// <summary>
        /// Deletes all existing visualizations from the scene.
        /// </summary>
        /// <param name="syncWithRemote">Indicates whether the visualization should also be deleted on remote clients.</param>
        public void DeleteAllVisualizations(bool syncWithRemote = true)
        {
            foreach (var kvp in Visualizations)
            {
                var settings = kvp.Value.Settings;
                kvp.Value.Dispose();

                // send notification event that a vis was deleted
                VisualizationDeletedEventBroadcast.Invoke(settings);
            }

            Visualizations.Clear();

            if (syncWithRemote)
            {
                var message = new MessageDeleteAllVisualizations();
                Services.NetworkManager().SendMessage(message.Pack());
            }
        }

        /// <summary>
        /// Deletes a single visualization with the provided <see cref="Guid"/>.
        /// </summary>
        /// <param name="visId">The <see cref="Guid"/> of the visualization.</param>
        /// <param name="syncWithRemote">Indicates whether the visualization should also be deleted on remote clients.</param>
        /// <returns>A bool indicating whether a visualization was successfully deleted.
        /// Returns <see langword="false"/> when there is no visualization with the provided <see cref="Guid"/>.</returns>
        public bool DeleteVisualization(Guid visId, bool syncWithRemote = true)
        {
            if (Visualizations.ContainsKey(visId))
            {
                var settings = Visualizations[visId].Settings;
                Visualizations[visId].Dispose();
                Visualizations.Remove(visId);
                if (syncWithRemote)
                {
                    var message = new MessageDeleteVisualization(visId);
                    Services.NetworkManager().SendMessage(message.Pack());
                }

                // send notification event that a vis was deleted
                VisualizationDeletedEventBroadcast.Invoke(settings);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new coordinate system.
        /// </summary>
        /// <param name="settings">The settings for the new coordinate system.</param>
        /// <returns>A <see cref="GameObject"/> with the coordinate system.</returns>
        public GameObject GenerateCoordinateSystemVis(VisProperties settings)
        {
            settings.VisId = Guid.Empty;
            settings.VisType = VisType.CoordinateSystem3D;
            GameObject vis;
            var coordinateSystem = Instantiate(CoordinateSystemPrefab);
            coordinateSystem.Init(settings);
            vis = coordinateSystem.gameObject;
            coordinateSystemVis = coordinateSystem;
            Transform worldAnchor = GameObject.FindGameObjectWithTag("VisRootAnchor").transform;
            vis.transform.SetParent(worldAnchor, false);
            return vis;
        }

        /// <summary>
        /// Returns a <see cref="List{Guid}"/> of all visualizations of a specified type.
        /// </summary>
        /// <param name="type">The <see cref="VisType"/> to search for.</param>
        /// <returns>A <see cref="List{Guid}"/> of visualizations with the provided <see cref="VisType"/>.</returns>
        public List<Guid> GetVisualizationsOfType(VisType type)
        {
            List<Guid> visList = new List<Guid>();
            foreach (var vis in Visualizations)
            {
                if (vis.Value.Settings.VisType == type)
                {
                    visList.Add(vis.Key);
                }
            }

            return visList;
        }

        /// <summary>
        /// Unity start function.
        /// </summary>
        public void Start()
        {
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.CREATE_VISUALIZATION, OnCreateVisualization);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.CREATE_CONTAINER, OnCreateVisContainer);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_VISUALIZATION, OnUpdateVisualization);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.DELETE_VISUALIZATION, OnDeleteVisualization);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.DELETE_ALL_VISUALIZATIONS, OnDeleteAllVisualizations);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.DELETE_ALL_CONTAINERS, OnDeleteAllContainers);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.CENTER_DATA, OnCenterData);

            Services.StudyManager().SessionFilterEventBroadcast.AddListener(OnSessionFilterChange);
            Services.StudyManager().StudyChangeBroadcast.AddListener(OnStudyLoaded);
        }

        /// <summary>
        /// Updates the session filter with the provided lists of sessions and conditions.
        /// </summary>
        /// <param name="sessions">The list of sessions.</param>
        /// <param name="conditions">The list of conditions.</param>
        public void UpdateSessionFilter(List<int> sessions, List<int> conditions)
        {
            foreach (var vis in Visualizations.Values)
            {
                vis.UpdateView(sessions, conditions);
            }

            if (!timelineController || timelineController.Disposed == true)
            {
                timelineController = null;
                var properties = new VisProperties(Guid.Empty, VisType.TimelineControl, -1)
                {
                    Conditions = new List<int>(conditions),
                    Sessions = new List<int>(sessions)
                };
                CreateTimelineControl(properties);
            }
            else
            {
                timelineController.UpdateView(sessions, conditions);
            }

            if (!coordinateSystemVis || coordinateSystemVis.Disposed == true)
            {
                coordinateSystemVis = null;
                var properties = new VisProperties(Guid.Empty, VisType.CoordinateSystem3D, -1)
                {
                    Conditions = new List<int>(conditions),
                    Sessions = new List<int>(sessions)
                };
                GenerateCoordinateSystemVis(properties);
            }
            else
            {
                coordinateSystemVis.UpdateView(sessions, conditions);
            }
        }

        /// <summary>
        /// Updates an existing visualization with the provided <see cref="VisProperties"/>.
        /// </summary>
        /// <param name="config">The struct containing the settings for the updated visualization.</param>
        /// <param name="syncWithRemote">Indicates whether this update should also happen on remote clients.</param>
        public void UpdateVisualization(VisProperties config, bool syncWithRemote = true)
        {
            if (Visualizations.TryGetValue(config.VisId, out AbstractView visualization))
            {
                visualization.UpdateView(config);
                if (syncWithRemote)
                {
                    var message = new MessageUpdateVisualization(config);
                    Services.NetworkManager().SendMessage(message.Pack());
                }
            }
        }

        /// <summary>
        /// Opens the settings UI for a given visualization.
        /// </summary>
        /// <param name="visId">The <see cref="Guid"/> identifying the visualization.</param>
        internal void OpenSettingsForVisualization(Guid visId)
        {
            if (Visualizations.ContainsKey(visId))
            {
                var visualization = Visualizations[visId];
                if (visualization is IConfigurableVisualization)
                {
                    ((IConfigurableVisualization)visualization).OpenSettingsUI();
                }
            }
        }

        private Task OnCenterData(MessageContainer obj)
        {
            MessageCenterData message = MessageCenterData.Unpack(obj);
            if (message != null)
            {
                CenterData(message.IsCentering, false);
            }

            return Task.CompletedTask;
        }

        private Task OnCreateVisContainer(MessageContainer obj)
        {
            MessageCreateVisContainer message = MessageCreateVisContainer.Unpack(obj);
            if (message != null)
            {
                CreateViewContainer(message.Container, false);
            }

            return Task.CompletedTask;
        }

        private Task OnCreateVisualization(MessageContainer obj)
        {
            MessageCreateVisualization message = MessageCreateVisualization.Unpack(obj);
            if (message != null)
            {
                CreateVisualization(message.Settings, false);
            }

            return Task.CompletedTask;
        }

        private Task OnDeleteAllContainers(MessageContainer obj)
        {
            MessageDeleteAllVisContainers message = MessageDeleteAllVisContainers.Unpack(obj);
            if (message != null)
            {
                DeleteAllViewContainers(false);
            }

            return Task.CompletedTask;
        }

        private Task OnDeleteAllVisualizations(MessageContainer obj)
        {
            MessageDeleteAllVisualizations message = MessageDeleteAllVisualizations.Unpack(obj);
            if (message != null)
            {
                DeleteAllVisualizations(false);
            }

            return Task.CompletedTask;
        }

        private Task OnDeleteVisualization(MessageContainer obj)
        {
            MessageDeleteVisualization message = MessageDeleteVisualization.Unpack(obj);
            if (message != null)
            {
                DeleteVisualization(message.VisId, false);
            }

            return Task.CompletedTask;
        }

        private void OnSessionFilterChange()
        {
            UpdateSessionFilter(Services.StudyManager().CurrentStudySessions, Services.StudyManager().CurrentStudyConditions);
        }

        private void OnStudyLoaded(int index)
        {
            DeleteAllVisualizations(false);
            if (timelineController)
            {
                timelineController.Dispose();
            }

            if (coordinateSystemVis)
            {
                coordinateSystemVis.Dispose();
            }
        }

        private Task OnUpdateVisualization(MessageContainer obj)
        {
            MessageUpdateVisualization message = MessageUpdateVisualization.Unpack(obj);
            if (message != null)
            {
                UpdateVisualization(message.Settings, false);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Invoked, whenever the data is centered or un-centered.
        /// </summary>
        public class DataCenteringEvent : UnityEvent<bool>
        {
        }

        /// <summary>
        /// Invoked, whenever a visualization is created.
        /// </summary>
        public class VisualizationCreatedEvent : UnityEvent<VisProperties>
        {
        }

        /// <summary>
        /// Invoked, whenever a visualization is deleted.
        /// </summary>
        public class VisualizationDeletedEvent : UnityEvent<VisProperties>
        {
        }
    }
}