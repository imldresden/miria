// ------------------------------------------------------------------------------------
// <copyright file="ViewContainer.cs" company="Technische Universität Dresden">
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
using IMLD.MixedRealityAnalysis.Core;
using IMLD.MixedRealityAnalysis.Network;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component stores one or more 2D visualizations and contains the logic to enable or disable them.
    /// </summary>
    public class ViewContainer : MonoBehaviour
    {
        public GameObject Background;
        public VisContainer Container;
        public Quaternion OrientationOffset = Quaternion.identity;

        public Vector3 PositionOffset = Vector3.zero;
        public Vector3 ScaleOffset = Vector3.one;
        public VisButton VisButtonPrefab;
        public GameObject VisButtonsGroup;
        private bool controlsVisible = true;
        private bool isVisRequested;
        private VisProperties requestedVis;
        private readonly List<GameObject> studyButtons = new List<GameObject>();
        private readonly List<VisButton> visButtons = new List<VisButton>();
        private readonly List<AbstractView> visualizations = new List<AbstractView>();

        private bool changedExternally = false;
        private Vector3 targetLocalPosition, previousLocalPosition;
        private Quaternion targetLocalRotation, previousLocalRotation;
        private Vector3 targetLocalScale, previousLocalScale;

        /// <summary>
        /// Gets the id of this view container.
        /// </summary>
        public int Id
        {
            get
            {
                if (Container != null)
                {
                    return Container.Id;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Gets the parent id of this view container.
        /// </summary>
        public int ParentId
        {
            get
            {
                if (Container != null)
                {
                    return Container.ParentId;
                }
                else
                {
                    return -1;
                }
            }
        }

        public void ChangePositionExternally(Vector3 localPosition, Quaternion localRotation)
        {
            targetLocalPosition = localPosition;
            targetLocalRotation = localRotation;
            targetLocalScale = transform.localScale;
            changedExternally = true;
        }

        public void UpdateTransform(Vector3 localPosition, Quaternion localRotation)
        {
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.hasChanged = true;
        }

        /// <summary>
        /// Attaches the provided visualization to this container.
        /// </summary>
        /// <param name="vis">The visualization that should be attached to this view container.</param>
        public void AttachVis(AbstractView vis)
        {
            vis.transform.SetParent(this.transform, false);
            HideControls();
            visualizations.Add(vis);
            foreach (var visButton in visButtons)
            {
                if (visButton.VisType == vis.VisType)
                {
                    visButton.SetActive(true);
                    return;
                }
            }
        }

        /// <summary>
        /// Disables and removes visualizations of a specified type from this view container.
        /// </summary>
        /// <param name="type">The type of visualization to remove from this view container.</param>
        public void DisableVis(VisType type)
        {
            var visList = GetVisualizationsOfType(type);
            foreach (var vis in visList)
            {
                visualizations.Remove(vis);
                Services.VisManager().DeleteVisualization(vis.VisId);
            }

            foreach (var visButton in visButtons)
            {
                if (visButton.VisType == type)
                {
                    visButton.SetActive(false);
                    return;
                }
            }
        }

        /// <summary>
        /// Gets a list of visualizations of a specified type that are attached to this <see cref="ViewContainer"/>.
        /// </summary>
        /// <param name="type">The type of visualization to get.</param>
        /// <returns>The list of visualizations of the specified type.</returns>
        public List<AbstractView> GetVisualizationsOfType(VisType type)
        {
            List<AbstractView> results = new List<AbstractView>();
            foreach (var vis in visualizations)
            {
                if (vis.Settings.VisType == type)
                {
                    results.Add(vis);
                }
            }

            return results;
        }

        /// <summary>
        /// Hides or shows controls when the background of the view container has been clicked.
        /// </summary>
        public void OnBackgroundButton()
        {
            if (controlsVisible)
            {
                HideControls();
            }
            else
            {
                ShowControls();
            }
        }

        /// <summary>
        /// Removes all visualizations from this view container.
        /// </summary>
        public void OnClearButton()
        {
            foreach (var vis in visualizations)
            {
                Services.VisManager().DeleteVisualization(vis.VisId);
            }

            visualizations.Clear();
        }

        /// <summary>
        /// Disables the visualization of type <see cref="VisType.Event2D"/>.
        /// </summary>
        public void OnEventVisDisabled()
        {
            DisableVis(VisType.Event2D);
        }

        /// <summary>
        /// Enables the visualization of type <see cref="VisType.Event2D"/>.
        /// </summary>
        public void OnEventVisEnabled()
        {
            if (GetVisualizationsOfType(VisType.Event2D).Count == 0)
            {
                // no visualization of this type exists in this ViewContainer
                List<int> dataSets = new List<int>();
                foreach (var kvp in Services.DataManager().DataSets)
                {
                    dataSets.Add(kvp.Key);
                }

                var properties = new VisProperties(Guid.Empty, VisType.Event2D, Id, dataSets, new List<int>(Services.StudyManager().CurrentStudyConditions), new List<int>(Services.StudyManager().CurrentStudySessions));
                RequestVisWhenReady(properties);
            }
        }

        /// <summary>
        /// Shows the settings for the visualization of type <see cref="VisType.Event2D"/>.
        /// </summary>
        public void OnEventVisSettings()
        {
            OpenSettingsUI(VisType.Event2D);
        }

        /// <summary>
        /// Disables the visualization of type <see cref="VisType.Heatmap2D"/>.
        /// </summary>
        public void OnHeatmapVisDisabled()
        {
            DisableVis(VisType.Heatmap2D);
        }

        /// <summary>
        /// Enables the visualization of type <see cref="VisType.Heatmap2D"/>.
        /// </summary>
        public void OnHeatmapVisEnabled()
        {
            if (GetVisualizationsOfType(VisType.Heatmap2D).Count == 0)
            {
                // no visualization of this type exists in this ViewContainer
                List<int> dataSets = new List<int>();
                foreach (var kvp in Services.DataManager().DataSets)
                {
                    if (kvp.Value.IsStatic == false)
                    {
                        dataSets.Add(kvp.Key);
                    }
                }

                var properties = new VisProperties(Guid.Empty, VisType.Heatmap2D, Id, dataSets, new List<int>(Services.StudyManager().CurrentStudyConditions), new List<int>(Services.StudyManager().CurrentStudySessions));
                RequestVisWhenReady(properties);
            }
        }

        /// <summary>
        /// Shows the settings for the visualization of type <see cref="VisType.Heatmap2D"/>.
        /// </summary>
        public void OnHeatmapVisSettings()
        {
            OpenSettingsUI(VisType.Heatmap2D);
        }

        /// <summary>
        /// Disables the visualization of type <see cref="VisType.Location2D"/>.
        /// </summary>
        public void OnLocationVisDisabled()
        {
            DisableVis(VisType.Location2D);
        }

        /// <summary>
        /// Enables the visualization of type <see cref="VisType.Location2D"/>.
        /// </summary>
        public void OnLocationVisEnabled()
        {
            if (GetVisualizationsOfType(VisType.Location2D).Count == 0)
            {
                // no visualization of this type exists in this ViewContainer
                List<int> dataSets = new List<int>();
                for (int i = 0; i < Services.DataManager().DataSets.Count; i++)
                {
                    if (Services.DataManager().DataSets[i].ObjectType == ObjectType.TOUCH)
                    {
                        dataSets.Add(i);
                    }
                }

                if (dataSets.Count == 0)
                {
                    foreach (var kvp in Services.DataManager().DataSets)
                    {
                        if (kvp.Value.IsStatic == false)
                        {
                            dataSets.Add(kvp.Key);
                        }
                    }
                }

                var properties = new VisProperties(Guid.Empty, VisType.Location2D, Id, dataSets, new List<int>(Services.StudyManager().CurrentStudyConditions), new List<int>(Services.StudyManager().CurrentStudySessions));
                RequestVisWhenReady(properties);
            }
        }

        /// <summary>
        /// Shows the settings for the visualization of type <see cref="VisType.Location2D"/>.
        /// </summary>
        public void OnLocationVisSettings()
        {
            OpenSettingsUI(VisType.Location2D);
        }

        /// <summary>
        /// Disables the visualization of type <see cref="VisType.Media2D"/>.
        /// </summary>
        public void OnMediaVisDisabled()
        {
            DisableVis(VisType.Media2D);
        }

        /// <summary>
        /// Enables the visualization of type <see cref="VisType.Media2D"/>.
        /// </summary>
        public void OnMediaVisEnabled()
        {
            if (GetVisualizationsOfType(VisType.Media2D).Count == 0)
            {
                // no visualization of this type exists in this ViewContainer
                List<int> dataSets = new List<int>();
                for (int i = 0; i < Services.DataManager().DataSets.Count; i++)
                {
                    if (Services.DataManager().DataSets[i].ObjectType == ObjectType.TOUCH)
                    {
                        dataSets.Add(i);
                    }
                }

                var properties = new VisProperties(Guid.Empty, VisType.Media2D, Id, dataSets, new List<int>(Services.StudyManager().CurrentStudyConditions), new List<int>(Services.StudyManager().CurrentStudySessions));
                RequestVisWhenReady(properties);
            }
        }

        /// <summary>
        /// Shows the settings for the visualization of type <see cref="VisType.Media2D"/>.
        /// </summary>
        public void OnMediaVisSettings()
        {
            OpenSettingsUI(VisType.Media2D);
        }

        /// <summary>
        /// Disables the visualization of type <see cref="VisType.Scatterplot2D"/>.
        /// </summary>
        public void OnScatterplotVisDisabled()
        {
            DisableVis(VisType.Scatterplot2D);
        }

        /// <summary>
        /// Enables the visualization of type <see cref="VisType.Scatterplot2D"/>.
        /// </summary>
        public void OnScatterplotVisEnabled()
        {
            if (GetVisualizationsOfType(VisType.Scatterplot2D).Count == 0)
            {
                // no visualization of this type exists in this ViewContainer
                List<int> dataSets = new List<int>();
                for (int i = 0; i < Services.DataManager().DataSets.Count; i++)
                {
                    if (Services.DataManager().DataSets[i].ObjectType == ObjectType.TOUCH)
                    {
                        dataSets.Add(i);
                    }
                }

                if (dataSets.Count == 0)
                {
                    foreach (var kvp in Services.DataManager().DataSets)
                    {
                        if (kvp.Value.IsStatic == false)
                        {
                            dataSets.Add(kvp.Key);
                        }
                    }
                }

                var properties = new VisProperties(Guid.Empty, VisType.Scatterplot2D, Id, dataSets, new List<int>(Services.StudyManager().CurrentStudyConditions), new List<int>(Services.StudyManager().CurrentStudySessions));
                RequestVisWhenReady(properties);
            }
        }

        /// <summary>
        /// Shows the settings for the visualization of type <see cref="VisType.Scatterplot2D"/>.
        /// </summary>
        public void OnScatterplotVisSettings()
        {
            OpenSettingsUI(VisType.Scatterplot2D);
        }

        /// <summary>
        /// Initializes this view container with the provided <see cref="VisContainer"/>.
        /// </summary>
        /// <param name="placeholder">The settings for this view container.</param>
        internal void Init(VisContainer placeholder)
        {
            Container = placeholder;

            if (placeholder.Position != null)
            {
                gameObject.transform.localPosition = new Vector3(placeholder.Position[0], placeholder.Position[1], placeholder.Position[2]);
            }
            else
            {
                gameObject.transform.localPosition = new Vector3(0, 0, 0);
            }

            PositionOffset = gameObject.transform.localPosition;

            if (placeholder.Orientation != null)
            {
                gameObject.transform.localRotation = new Quaternion(placeholder.Orientation[0], placeholder.Orientation[1], placeholder.Orientation[2], placeholder.Orientation[3]);
            }
            else
            {
                gameObject.transform.localRotation = Quaternion.identity;
            }

            OrientationOffset = gameObject.transform.localRotation;

            if (placeholder.Scale != null)
            {
                gameObject.transform.localScale = new Vector3(placeholder.Scale[0], placeholder.Scale[1], placeholder.Scale[2]);
            }
            else
            {
                gameObject.transform.localScale = new Vector3(1, 1, 1);
            }

            ScaleOffset = gameObject.transform.localScale;

            if (VisButtonsGroup != null)
            {
                Vector3 vec = new Vector3(1.0f / gameObject.transform.localScale.x, 1.0f / gameObject.transform.localScale.y, 1.0f / gameObject.transform.localScale.z);
                VisButtonsGroup.transform.localScale = vec;
            }
        }

        //private Task OnRemoteUpdate(MessageContainer obj)
        //{
        //    var message = MessageUpdateVisContainer.Unpack(obj);

        //    targetLocalPosition = message.Position;
        //    targetLocalRotation = message.Orientation;
        //    targetLocalScale = new Vector3(message.Scale, message.Scale, message.Scale);
        //    changedExternally = true;

        //    return Task.CompletedTask;
        //}

        private void HideControls()
        {
            if (Background)
            {
                Background.GetComponent<Renderer>().enabled = false;
            }

            if (VisButtonsGroup)
            {
                VisButtonsGroup.SetActive(false);
            }

            controlsVisible = false;
        }

        private void OnVisualizationCreated(VisProperties settings)
        {
            foreach (var visButton in visButtons)
            {
                if (visButton.VisType == (VisType)settings.VisType)
                {
                    visButton.SetActive(true);
                    return;
                }
            }
        }

        private void OnVisualizationDeleted(VisProperties settings)
        {
            foreach (var visButton in visButtons)
            {
                if (visButton.VisType == (VisType)settings.VisType)
                {
                    visButton.SetActive(false);
                    return;
                }
            }
        }

        private void OpenSettingsUI(VisType type)
        {
            var guidList = Services.VisManager().GetVisualizationsOfType(type);
            foreach (var guid in guidList)
            {
                Services.VisManager().OpenSettingsForVisualization(guid);
            }
        }

        private void RequestVisWhenReady(VisProperties visSettingsStruct)
        {
            requestedVis = visSettingsStruct;
            isVisRequested = true;
        }

        private void ShowControls()
        {
            if (Background)
            {
                Background.GetComponent<Renderer>().enabled = true;
            }

            if (VisButtonsGroup)
            {
                VisButtonsGroup.SetActive(true);
            }

            controlsVisible = true;
        }

        // Start is called before the first frame update
        private void Start()
        {
            // generate visualization buttons
            Vector3 z_offset = new Vector3(0f, 0f, -0.005f);
            foreach (var prefab in Services.VisManager().VisualizationPrefabs)
            {
                if (prefab.Is3D == false && prefab is ViewTimelineControl == false)
                {
                    var visButton = Instantiate(VisButtonPrefab, VisButtonsGroup.transform);
                    visButton.VisType = prefab.VisType;
                    visButton.AnchorId = this.Id;
                    visButton.transform.position += z_offset;
                    var helper = visButton.SpawnButton.GetComponent<ButtonConfigHelper>();

                    if (helper)
                    {
                        helper.SeeItSayItLabelEnabled = false;
                        helper.MainLabelText = prefab.VisType.ToString();
                        helper.SetSpriteIconByName("Icon2D");
                    }

                    visButtons.Add(visButton);
                }
            }

            var collection = VisButtonsGroup.gameObject.GetComponent<GridObjectCollection>();
            collection.UpdateCollection();

            //Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_CONTAINER, OnRemoteUpdate);

            previousLocalPosition = transform.localPosition;
            previousLocalRotation = transform.localRotation;
            previousLocalScale = transform.localScale;
        }

        // Update is called once per frame
        private void Update()
        {
            if (changedExternally)
            {
                transform.localPosition = targetLocalPosition;
                transform.localRotation = targetLocalRotation;
                transform.localScale = targetLocalScale;
                changedExternally = false;
                transform.hasChanged = true;
            }
            //else if (previousLocalPosition != transform.localPosition || previousLocalRotation != transform.localRotation || previousLocalScale != transform.localScale)
            //{
            //    var message = new MessageUpdateVisContainer(Container).Pack();
            //    Services.NetworkManager().SendMessage(message);
            //}

            previousLocalPosition = transform.localPosition;
            previousLocalRotation = transform.localRotation;
            previousLocalScale = transform.localScale;

            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                if (visualizations != null)
                {
                    foreach (var vis in visualizations)
                    {
                        vis.UpdateView();
                    }
                }
            }

            if (isVisRequested)
            {
                isVisRequested = false;
                Services.VisManager().CreateVisualization(requestedVis);
            }
        }
    }
}