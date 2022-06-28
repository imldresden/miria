// ------------------------------------------------------------------------------------
// <copyright file="ProgressIndicator.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component shows a loading animation that can be used during loading times.
    /// </summary>
    public class ProgressIndicator : MonoBehaviour
    {
        /// <summary>
        /// Instance reference for the Singleton pattern.
        /// </summary>
        public static ProgressIndicator Instance = null;

        public ProgressIndicatorOrbsRotator ProgressBar;

        /// <summary>
        /// Starts the loading animation. Shows the provided text message.
        /// </summary>
        /// <param name="message">The message to show during loading.</param>
        /// <returns>A Task object.</returns>
        public static async Task StartProgressIndicator(string message = "Loading...")
        {
            if (Instance.ProgressBar != null)
            {
                Instance.ProgressBar.Message = message;
                if (Instance.ProgressBar.State == ProgressIndicatorState.Closed)
                {
                    await Instance.ProgressBar.OpenAsync();
                }
            }
        }

        /// <summary>
        /// Stops the loading animation.
        /// </summary>
        /// <returns>A Task object.</returns>
        public static async Task StopProgressIndicator()
        {
            if (Instance.ProgressBar != null)
            {
                if (Instance.ProgressBar.State == ProgressIndicatorState.Open)
                {
                    await Instance.ProgressBar.CloseAsync();
                }
                else
                {
                    Instance.ProgressBar.StopOrbs();
                    Instance.ProgressBar.gameObject.SetActive(false);
                }
            }
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

        private void Start()
        {
            if (Instance.ProgressBar != null)
            {
                Instance.ProgressBar.gameObject.SetActive(false);
            }
        }
    }
}