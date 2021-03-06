﻿// ------------------------------------------------------------------------------------
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

        /// <summary>
        /// Stores a reference to a <see cref="SimpleAnchorManager"/>.
        /// </summary>
        [SerializeField]
        private SimpleAnchorManager AnchorManagerReference;

        /// <summary>
        /// Stores a reference to an <see cref="AbstractDataProvider"/>.
        /// </summary>
        [SerializeField]
        private AbstractDataProvider DataManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="NetworkManagerJson"/>.
        /// </summary>
        [SerializeField]
        private NetworkManagerJson NetworkManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="StudyManager"/>.
        /// </summary>
        [SerializeField]
        private StudyManager StudyManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="UserIndicatorManager"/>.
        /// </summary>
        [SerializeField]
        private UserIndicatorManager UserManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="ViewContainerManager"/>.
        /// </summary>
        [SerializeField]
        private ViewContainerManager ViewContainerManagerReference;

        /// <summary>
        /// Stores a reference to a <see cref="VisualizationManager"/>.
        /// </summary>
        [SerializeField]
        private VisualizationManager VisManagerReference;

        /// <summary>
        /// Returns the <see cref="SimpleAnchorManager"/>.
        /// </summary>
        /// <returns>The <see cref="SimpleAnchorManager"/>.</returns>
        public static SimpleAnchorManager AnchorManager()
        {
            return Instance.AnchorManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="ViewContainerManager"/>.
        /// </summary>
        /// <returns>The <see cref="ViewContainerManager"/>.</returns>
        public static ViewContainerManager ContainerManager()
        {
            return Instance.ViewContainerManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="AbstractDataProvider"/>.
        /// </summary>
        /// <returns>The <see cref="AbstractDataProvider"/>.</returns>
        public static AbstractDataProvider DataManager()
        {
            return Instance.DataManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="NetworkManagerJson"/>.
        /// </summary>
        /// <returns>The <see cref="NetworkManagerJson"/>.</returns>
        public static NetworkManagerJson NetworkManager()
        {
            return Instance.NetworkManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="StudyManager"/>.
        /// </summary>
        /// <returns>The <see cref="StudyManager"/>.</returns>
        public static StudyManager StudyManager()
        {
            return Instance.StudyManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="UserIndicatorManager"/>.
        /// </summary>
        /// <returns>The <see cref="UserIndicatorManager"/>.</returns>
        public static UserIndicatorManager UserManager()
        {
            return Instance.UserManagerReference;
        }

        /// <summary>
        /// Returns the <see cref="VisualizationManager"/>.
        /// </summary>
        /// <returns>The <see cref="VisualizationManager"/>.</returns>
        public static VisualizationManager VisManager()
        {
            return Instance.VisManagerReference;
        }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }
    }
}