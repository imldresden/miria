// ------------------------------------------------------------------------------------
// <copyright file="VisProperties.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This class stores the settings for a visualization.
    /// </summary>
    public class VisProperties
    {
        [JsonProperty]
        private Dictionary<string, object> Properties = new Dictionary<string, object>();

        [JsonConstructor]
        private VisProperties(Guid visId, VisType visType, int anchorId, List<int> objectids, List<int> conditions, List<int> sessions, Dictionary<string, object> properties) : this(visId, visType, anchorId, objectids, conditions, sessions)
        {
            Properties = properties;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisProperties"/> class.
        /// </summary>
        /// <param name="visId">The <see cref="Guid"/> of the visualization.</param>
        /// <param name="visType">The type of the visualization, match a <see cref="VisType"/>.</param>
        /// <param name="anchorId">The id of the <see cref="ViewContainer"/> of the visualization.</param>
        public VisProperties(Guid visId, VisType visType, int anchorId)
        {
            VisId = visId;
            VisType = visType;
            AnchorId = anchorId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisProperties"/> class.
        /// </summary>
        /// <param name="visId">The <see cref="Guid"/> of the visualization.</param>
        /// <param name="visType">The type of the visualization, match a <see cref="VisType"/>.</param>
        /// <param name="anchorId">The id of the <see cref="ViewContainer"/> of the visualization.</param>
        /// <param name="objectids">A <see cref="List{int}"/> representing the <see cref="AnalysisObject"/> ids of the visualization.</param>
        /// <param name="conditions">A <see cref="List{int}"/> representing the study conditions of the visualization</param>
        /// <param name="sessions">A <see cref="List{int}"/> representing the study sessions of the visualization</param>
        public VisProperties(Guid visId, VisType visType, int anchorId, List<int> objectids, List<int> conditions, List<int> sessions)
        {
            VisId = visId;
            VisType = visType;
            AnchorId = anchorId;
            ObjectIds = objectids;
            Conditions = conditions;
            Sessions = sessions;
        }

        /// <summary>
        /// Gets or sets the anchor id of the visualization.
        /// </summary>
        public int AnchorId { get; set; }

        /// <summary>
        /// Gets or sets the list of conditions of the visualization.
        /// </summary>
        public List<int> Conditions { get; set; }

        /// <summary>
        /// Gets or sets the list of analysis objects of the visualization.
        /// </summary>
        public List<int> ObjectIds { get; set; }

        /// <summary>
        /// Gets or sets the list of sessions of the visualization.
        /// </summary>
        public List<int> Sessions { get; set; }

        /// <summary>
        /// Gets or sets the id of the visualization.
        /// </summary>
        public Guid VisId { get; set; }

        /// <summary>
        /// Gets or sets the type of the visualization.
        /// </summary>
        public VisType VisType { get; set; }

        /// <summary>
        /// Gets the property with the specified name. If the property may not exist, use <see cref="TryGet{T}(string, out T)"/> instead.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The property if it exists.</returns>
        /// <exception cref="KeyNotFoundException"><paramref name="propertyName"/> is not a valid key.</exception>
        public object Get(string propertyName)
        {
            // get property
            return Properties[propertyName];
        }

        /// <summary>
        /// Sets the property of the specified name.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The value of the property.</param>
        public void Set(string propertyName, object propertyValue)
        {
            // set property
            Properties[propertyName] = propertyValue;
        }

        /// <summary>
        /// Gets the property with the specified name and type.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The value of the property if it was found; otherwise, null.</param>
        /// <returns><c>true</c> if the property is found, <c>false</c> otherwise.</returns>
        public bool TryGet(string propertyName, out object propertyValue)
        {
            // get property
            if (Properties.TryGetValue(propertyName, out object value))
            {
                propertyValue = value;
                return true;
            }

            propertyValue = null;
            return false;
        }

        /// <summary>
        /// Gets the property with the specified name and type.
        /// </summary>
        /// <typeparam name="T">The type of the expected output.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The value of the property if it was found; otherwise, teh default for T.</param>
        /// <returns><c>true</c> if the property is found, <c>false</c> otherwise.</returns>
        public bool TryGet<T>(string propertyName, out T propertyValue)
        {
            // get property
            if (Properties.TryGetValue(propertyName, out object value))
            {
                if(value is T)
                {
                    propertyValue = (T)value;
                    return true;
                }
                
            }

            propertyValue = default(T);
            return false;
        }
    }
}