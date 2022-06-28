// ------------------------------------------------------------------------------------
// <copyright file="Services.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using IMLD.MixedRealityAnalysis.Network;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This component makes core services available to other components.
    /// </summary>
    public class Services : MonoBehaviour
    {
        /// <summary>
        /// The Instance field to implement the Singleton pattern.
        /// </summary>
        public static Services Instance = null;

        ///// <summary>
        ///// Stores a reference to a <see cref="SimpleAnchorManager"/>.
        ///// </summary>
        //public SimpleAnchorManager AnchorManagerReference;

        /// <summary>
        /// Stores a reference to an <see cref="AbstractDataProvider"/>.
        /// </summary>
        public AbstractDataProvider DataManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="Network.NetworkManager"/>.
        /// </summary>
        public NetworkManager NetworkManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="StudyManager"/>.
        /// </summary>
        public StudyManager StudyManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="UserIndicatorManager"/>.
        /// </summary>
        public UserIndicatorManager UserManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="ViewContainerManager"/>.
        /// </summary>
        public ViewContainerManager ViewContainerManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="VisualizationManager"/>.
        /// </summary>
        public VisualizationManager VisManagerReference;

        ///// <summary>
        ///// Returns the <see cref="SimpleAnchorManager"/>.
        ///// </summary>
        ///// <returns>The <see cref="SimpleAnchorManager"/>.</returns>
        //public static SimpleAnchorManager AnchorManager()
        //{
        //    if(Instance == null)
        //    {
        //        return null;
        //    }

        //    return Instance.AnchorManagerReference;
        //}

        /// <summary>
        /// Returns the <see cref="ViewContainerManager"/>.
        /// </summary>
        /// <returns>The <see cref="ViewContainerManager"/>.</returns>
        public static ViewContainerManager ContainerManager()
        {
            if (Instance == null)
            {
                return null;
            }

            return Instance.ViewContainerManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="AbstractDataProvider"/>.
        /// </summary>
        /// <returns>The <see cref="AbstractDataProvider"/>.</returns>
        public static AbstractDataProvider DataManager()
        {
            if (Instance == null)
            {
                return null;
            }

            return Instance.DataManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="Network.NetworkManager"/>.
        /// </summary>
        /// <returns>The <see cref="Network.NetworkManager"/>.</returns>
        public static NetworkManager NetworkManager()
        {
            if (Instance == null)
            {
                return null;
            }

            return Instance.NetworkManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="StudyManager"/>.
        /// </summary>
        /// <returns>The <see cref="StudyManager"/>.</returns>
        public static StudyManager StudyManager()
        {
            if (Instance == null)
            {
                return null;
            }

            return Instance.StudyManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="UserIndicatorManager"/>.
        /// </summary>
        /// <returns>The <see cref="UserIndicatorManager"/>.</returns>
        public static UserIndicatorManager UserManager()
        {
            if (Instance == null)
            {
                return null;
            }

            return Instance.UserManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="VisualizationManager"/>.
        /// </summary>
        /// <returns>The <see cref="VisualizationManager"/>.</returns>
        public static VisualizationManager VisManager()
        {
            if (Instance == null)
            {
                return null;
            }

            return Instance.VisManagerReference;
        }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}