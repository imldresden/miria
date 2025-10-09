// ------------------------------------------------------------------------------------
// <copyright file="PlayButton.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    // TODO: MRTKv3 migration

    /// <summary>
    /// This Unity component is used to start and pause playback in the time line.
    /// </summary>
    public class PlayButton : MonoBehaviour
    {
        public Sprite PlaySprite, PauseSprite;
        private SpriteRenderer icon;
        private ViewTimelineControl timelineControl;

        /// <summary>
        /// Implements IMixedRealityPointerHandler. Toggles playback.
        /// </summary>
        /// <param name="eventData">The click event data.</param>
        public void OnPointerClicked()
        {
            if (timelineControl.TimelineStatus == TimelineStatus.PAUSED)
            {
                timelineControl.StartPlayback();
            }
            else if (timelineControl.TimelineStatus == TimelineStatus.PLAYING)
            {
                timelineControl.PausePlayback();
            }
        }

        /// <summary>
        /// Updates the button state based on the time line state.
        /// </summary>
        public void UpdateButton()
        {
            if (timelineControl == null)
            {
                Start();
            }

            if (timelineControl.TimelineStatus == TimelineStatus.PAUSED)
            {
                icon.sprite = PlaySprite;
            }
            else if (timelineControl.TimelineStatus == TimelineStatus.PLAYING)
            {
                icon.sprite = PauseSprite;
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            timelineControl = gameObject.GetComponentInParent<ViewTimelineControl>();
            icon = transform.Find("IconImage").GetComponent<SpriteRenderer>();
        }
    }
}