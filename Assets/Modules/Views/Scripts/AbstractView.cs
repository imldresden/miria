// ------------------------------------------------------------------------------------
// <copyright file="AbstractView.cs" company="Technische Universität Dresden">
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
    /// This abstract Unity component is the base for all visualization views.
    /// </summary>
    public abstract class AbstractView : MonoBehaviour
    {
        /// <summary>
        /// The anchor transform of this view. Every view has one anchor point and is positioned and oriented relative to this anchor.
        /// </summary>
        public Transform Anchor;

        /// <summary>
        /// The unique id of this visualization.
        /// </summary>
        public Guid VisId;

        /// <summary>
        /// Gets a value indicating whether this visualization has been disposed by calling <see cref="Dispose"/>.
        /// </summary>
        public bool Disposed { get; private set; } = false;

        /// <summary>
        /// Gets or sets the settings object for this visualization.
        /// </summary>
        public VisProperties Settings { get; protected set; }

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public abstract VisType VisType { get; }

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public abstract bool Is3D { get; }

        /// <summary>
        /// Disposes the view.
        /// </summary>
        public virtual void Dispose()
        {
            gameObject.SetActive(false);
            Disposed = true;

            Destroy(gameObject);
        }

        /// <summary>
        /// Initializes the view with the provided settings.
        /// </summary>
        /// <param name="settings">The settings to use for this view.</param>
        public abstract void Init(VisProperties settings);

        /// <summary>
        /// Updates the view.
        /// </summary>
        public abstract void UpdateView();

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public abstract void UpdateView(VisProperties settings);

        /// <summary>
        /// Updates the view by setting the sessions and conditions to the provided lists.
        /// </summary>
        /// <param name="sessions">The list of session ids.</param>
        /// <param name="conditions">The list of condition ids.</param>
        public virtual void UpdateView(List<int> sessions, List<int> conditions)
        {
            if(Settings != null)
            {
                Settings.Sessions = sessions;
                Settings.Conditions = conditions;
            }

            UpdateView();
        }

        /// <summary>
        /// Takes the provided properties and sets defaults where necessary.
        /// </summary>
        /// <param name="properties">The properties for the view.</param>
        /// <returns>The modified <see cref="VisProperties"/>.</returns>
        protected virtual VisProperties ParseSettings(VisProperties properties)
        {
            // set vis id
            if (properties.VisId == Guid.Empty)
            {
                properties.VisId = this.VisId;
            }

            // set vis type
            properties.VisType = this.VisType;

            if (properties.ObjectIds == null)
            {
                // not set, keep current ones
                if (Settings == null || Settings.ObjectIds == null)
                {
                    properties.ObjectIds = new List<int>();
                    for (int i = 0; i < Services.DataManager().CurrentStudy.Objects.Count; i++)
                    {
                        properties.ObjectIds.Add(Services.DataManager().CurrentStudy.Objects[i].Id);
                    }
                }
                else
                {
                    properties.ObjectIds = Settings.ObjectIds;
                }
            }
            else if (properties.ObjectIds.Count == 1 && properties.ObjectIds[0] == -1)
            {
                // set to -1, use all objects
                properties.ObjectIds = new List<int>();
                for (int i = 0; i < Services.DataManager().CurrentStudy.Objects.Count; i++)
                {
                    properties.ObjectIds.Add(Services.DataManager().CurrentStudy.Objects[i].Id);
                }
            }

            // parse conditions
            if (properties.Conditions == null)
            {
                // not set, keep current conditions                
                if (Settings == null || Settings.Conditions == null)
                {
                    properties.Conditions = new List<int>(Services.StudyManager().CurrentStudyConditions);
                }
                else
                {
                    properties.Conditions = Settings.Conditions;
                }
            }
            else if (properties.Conditions.Count == 0 || (properties.Conditions.Count == 1 && properties.Conditions[0] == -1))
            {
                // set to -1, use all conditions
                properties.Conditions = new List<int>();
                for (int i = 0; i < Services.DataManager().CurrentStudy.Conditions.Count; i++)
                {
                    properties.Conditions.Add(i);
                }
            }

            // parse sessions
            if (properties.Sessions == null)
            {
                // not set, keep current sessions
                if (Settings == null || Settings.Sessions == null)
                {
                    properties.Sessions = new List<int>(Services.StudyManager().CurrentStudySessions);
                }
                else
                {
                    properties.Sessions = Settings.Sessions;
                }
            }
            else if (properties.Sessions.Count == 0 || (properties.Sessions.Count == 1 && properties.Sessions[0] == -1))
            {
                // set to -1, use all sessions
                properties.Sessions = new List<int>();
                for (int i = 0; i < Services.DataManager().CurrentStudy.Sessions.Count; i++)
                {
                    properties.Sessions.Add(i);
                }
            }

            return properties;
        }
    }
}