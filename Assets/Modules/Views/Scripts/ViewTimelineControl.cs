// ------------------------------------------------------------------------------------
// <copyright file="ViewTimelineControl.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Globalization;
using IMLD.MixedRealityAnalysis.Core;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Video;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component implements <see cref="AbstractView"/> and is a view for the timeline controls.
    /// </summary>
    public class ViewTimelineControl : AbstractView
    {
        public GameObject CurrentTimeLabel;
        public Vis2DEvents EventVisualization;
        public Interactable IncreaseSpeedButton;
        public GameObject MaxTimeFilterLabel;
        public GameObject MinTimeFilterLabel;
        public Sprite PauseSprite;
        public float PlaybackSpeed = 1;
        public TMPro.TextMeshPro PlaybackSpeedLabel;
        public Interactable PlayButton;
        public Sprite PlaySprite;
        public Interactable ReduceSpeedButton;
        public float SpeedMultiplier = 2.0f;

        private long currentTimeFilterMax = long.MinValue;
        private long currentTimeFilterMin = long.MaxValue;
        private long displayedTimestamp;
        private TimeFilterSliderGestureControl filterSliderGestureControl;
        private bool isInitialized = false;
        private long maxTimestamp = long.MinValue;
        private long minTimestamp = long.MaxValue;
        private TimelineScale scale;
        private TimeSliderGestureControl sliderGestureControl;
        ////private Transform sliderKnob;
        private StudyManager studyManager;
        private float timeFilterMax = 1;
        private float timeFilterMin = 0;
        private float timelineProgress;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => false;

        /// <summary>
        /// Gets the current timeline status.
        /// </summary>
        public TimelineStatus TimelineStatus
        {
            get { return studyManager.TimelineStatus; }
        }

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.TimelineControl;

        /// <summary>
        /// Initializes this view with the provided settings.
        /// </summary>
        /// <param name="settings">The settings for the view.</param>
        public override void Init(VisProperties settings)
        {
            if (isInitialized)
            {
                Reset();
            }

            if (Services.DataManager() == null || Services.DataManager().CurrentStudy == null)
            {
                // no study loaded
                return; // vis is now reset, we return because there is nothing to load
            }

            Settings = ParseSettings(settings); // parse the settings from the settings object, also makes a deep copy
            VisId = Settings.VisId;

            // set callbacks for UI buttons
            if (PlayButton)
            {
                PlayButton.OnClick.AddListener(TogglePlayback);
            }

            if (ReduceSpeedButton)
            {
                ReduceSpeedButton.OnClick.AddListener(ReducePlaybackSpeed);
            }

            if (IncreaseSpeedButton)
            {
                IncreaseSpeedButton.OnClick.AddListener(IncreasePlaybackSpeed);
            }

            studyManager = Services.StudyManager();
            studyManager.TimelineEventBroadcast.AddListener(TimelineUpdated);
            studyManager.TimeFilterEventBroadcast.AddListener(TimeFilterUpdated);
            sliderGestureControl = GetComponentInChildren<TimeSliderGestureControl>();
            scale = GetComponentInChildren<TimelineScale>();
            sliderGestureControl.OnUpdateEvent.AddListener(UpdatedSlider);
            filterSliderGestureControl = GetComponentInChildren<TimeFilterSliderGestureControl>();
            filterSliderGestureControl.OnUpdateEvent.AddListener(UpdatedFilterSlider);

            scale.Init(0.9f);
            isInitialized = true;

            UpdateView();
            UpdateEventVis(settings);
        }

        /// <summary>
        /// Pauses playback of the timeline.
        /// </summary>
        public void PausePlayback()
        {
            if (!isInitialized)
            {
                return;
            }

            Services.StudyManager().StopPlayback();
        }

        /// <summary>
        /// Starts playback of the timeline.
        /// </summary>
        public void StartPlayback()
        {
            if (!isInitialized)
            {
                return;
            }

            Services.StudyManager().StartPlayback();
        }

        /// <summary>
        /// Updates the view.
        /// </summary>
        public override void UpdateView()
        {
            if (!isInitialized)
            {
                return;
            }

            // set min and max time stamp for the timeline and the time filter
            minTimestamp = studyManager.MinTimestamp;
            maxTimestamp = studyManager.MaxTimestamp;
            currentTimeFilterMin = studyManager.CurrentTimeFilter.MinTimestamp;
            currentTimeFilterMax = studyManager.CurrentTimeFilter.MaxTimestamp;

            // set the max label of the time scale accordingly
            scale.SetMaxLabel(TimeSpan.FromTicks(maxTimestamp - minTimestamp).ToString(@"mm\:ss"));

            // set the current time indicator label
            displayedTimestamp = Math.Max(Math.Min(studyManager.CurrentTimestamp, maxTimestamp), minTimestamp);
            CurrentTimeLabel.GetComponent<TMPro.TextMeshPro>().text = TimeSpan.FromTicks(displayedTimestamp).ToString(@"mm\:ss");

            // set the min and max label of the filter
            MinTimeFilterLabel.GetComponent<TMPro.TextMeshPro>().text = TimeSpan.FromTicks(currentTimeFilterMin).ToString(@"mm\:ss");
            MaxTimeFilterLabel.GetComponent<TMPro.TextMeshPro>().text = TimeSpan.FromTicks(currentTimeFilterMax).ToString(@"mm\:ss");
            timelineProgress = (float)(displayedTimestamp - minTimestamp) / (maxTimestamp - minTimestamp); // between 0 and 1

            // set the progress on the time scale
            if (float.IsNaN(timelineProgress))
            {
                timelineProgress = 0;
            }

            sliderGestureControl.SetSliderValue(timelineProgress);

            // update "physical" slider knob position
            Vector3 newSliderPosition = sliderGestureControl.transform.GetChild(1).localPosition;
            newSliderPosition.x = Mathf.Clamp(timelineProgress - 0.5f, -0.5f, 0.5f);
            sliderGestureControl.transform.GetChild(1).localPosition = newSliderPosition;

            // update slider values for the filter sliders
            filterSliderGestureControl.SetMinSliderValue(timeFilterMin);
            filterSliderGestureControl.SetMaxSliderValue(timeFilterMax);

            // update "physical" filter slider knob positions
            Vector3 newFilterSliderMinPosition = filterSliderGestureControl.LeftSlider.transform.localPosition;
            newFilterSliderMinPosition.x = Mathf.Clamp(timeFilterMin - 0.5f, -0.5f, 0.5f);
            filterSliderGestureControl.LeftSlider.transform.localPosition = newFilterSliderMinPosition;
            Vector3 newFilterSliderMaxPosition = filterSliderGestureControl.RightSlider.transform.localPosition;
            newFilterSliderMaxPosition.x = Mathf.Clamp(timeFilterMax - 0.5f, -0.5f, 0.5f);
            filterSliderGestureControl.RightSlider.transform.localPosition = newFilterSliderMaxPosition;
        }

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public override void UpdateView(VisProperties settings)
        {
            Init(settings);
        }

        private void IncreasePlaybackSpeed()
        {
            if (PlaybackSpeed < 8)
            {
                PlaybackSpeed *= SpeedMultiplier;
            }

            foreach (VideoPlayer vp in GameObject.FindObjectsOfType<VideoPlayer>())
            {
                vp.playbackSpeed = PlaybackSpeed;
            }

            Services.StudyManager().SetPlaybackSpeed(PlaybackSpeed);

            if (PlaybackSpeedLabel)
            {
                PlaybackSpeedLabel.text = "x" + PlaybackSpeed.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void ReducePlaybackSpeed()
        {
            if (PlaybackSpeed > 0.25)
            {
                PlaybackSpeed /= SpeedMultiplier;
            }

            foreach (VideoPlayer vp in GameObject.FindObjectsOfType<VideoPlayer>())
            {
                vp.playbackSpeed = PlaybackSpeed;
            }

            Services.StudyManager().SetPlaybackSpeed(PlaybackSpeed);

            if (PlaybackSpeedLabel)
            {
                PlaybackSpeedLabel.text = "x" + PlaybackSpeed.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void Reset()
        {
            // remove event listeners
            if (studyManager)
            {
                studyManager.TimelineEventBroadcast.RemoveListener(TimelineUpdated);
            }

            if (sliderGestureControl)
            {
                sliderGestureControl.OnUpdateEvent.RemoveListener(UpdatedSlider);
            }

            // remove callbacks for UI buttons
            if (PlayButton)
            {
                PlayButton.OnClick.RemoveAllListeners();
            }

            if (ReduceSpeedButton)
            {
                ReduceSpeedButton.OnClick.RemoveAllListeners();
            }

            if (IncreaseSpeedButton)
            {
                IncreaseSpeedButton.OnClick.RemoveAllListeners();
            }

            minTimestamp = long.MaxValue;
            maxTimestamp = long.MinValue;
            isInitialized = false;
        }

        private void TimeFilterUpdated(TimeFilter timeFilter)
        {
            if (!isInitialized)
            {
                return;
            }

            timeFilterMin = timeFilter.MinTime;
            timeFilterMax = timeFilter.MaxTime;
            currentTimeFilterMin = timeFilter.MinTimestamp;
            currentTimeFilterMax = timeFilter.MaxTimestamp;
            UpdateView();
        }

        private void TimelineUpdated(TimelineState timelineState)
        {
            if (!isInitialized)
            {
                return;
            }

            if (PlayButton)
            {
                var helper = PlayButton.GetComponent<ButtonConfigHelper>();
                if (helper)
                {
                    if (TimelineStatus == TimelineStatus.PAUSED)
                    {
                        helper.MainLabelText = "Play";
                        helper.SetSpriteIconByName("IconPlay");
                    }
                    else if (TimelineStatus == TimelineStatus.PLAYING)
                    {
                        helper.MainLabelText = "Pause";
                        helper.SetSpriteIconByName("IconPause");
                    }
                }
            }

            PlaybackSpeed = timelineState.PlaybackSpeed;
            foreach (VideoPlayer vp in GameObject.FindObjectsOfType<VideoPlayer>())
            {
                vp.playbackSpeed = PlaybackSpeed;
            }

            if (PlaybackSpeedLabel)
            {
                PlaybackSpeedLabel.text = "x" + PlaybackSpeed.ToString(CultureInfo.InvariantCulture);
            }

            UpdateView();
        }

        private void TogglePlayback()
        {
            if (TimelineStatus == TimelineStatus.PAUSED)
            {
                StartPlayback();
            }
            else if (TimelineStatus == TimelineStatus.PLAYING)
            {
                PausePlayback();
            }
        }

        private void Update()
        {
            if (isInitialized)
            {
                // update time stamp if necessary
                if (studyManager.TimelineStatus == TimelineStatus.PLAYING)
                {
                    studyManager.CurrentTimestamp += (long)(Time.deltaTime * 10000000 * PlaybackSpeed);
                    studyManager.CurrentTimestamp = Math.Max(Math.Min(studyManager.CurrentTimestamp, currentTimeFilterMax), currentTimeFilterMin); // clamp to min/max
                    if (studyManager.CurrentTimestamp == currentTimeFilterMax)
                    {
                        PausePlayback(); // pause/stop playback if timeline has reached its end
                    }
                }

                // update timeline bar if time stamp changed
                if (displayedTimestamp != studyManager.CurrentTimestamp)
                {
                    UpdateView();
                }
            }
        }

        private void UpdatedFilterSlider()
        {
            // update time filter over network
            studyManager.UpdateTimeFilter(filterSliderGestureControl.SliderValueMin, filterSliderGestureControl.SliderValueMax);

            // update time slider locally (if necessary), will trigger network update
            if (sliderGestureControl.SliderValue < filterSliderGestureControl.SliderValueMin)
            {
                sliderGestureControl.SliderValue = filterSliderGestureControl.SliderValueMin;
            }
            else if (sliderGestureControl.SliderValue > filterSliderGestureControl.SliderValueMax)
            {
                sliderGestureControl.SliderValue = filterSliderGestureControl.SliderValueMax;
            }
        }

        private void UpdatedSlider()
        {
            // update slider value over network
            studyManager.UpdateTimeline(studyManager.TimelineStatus, minTimestamp + (long)((maxTimestamp - minTimestamp) * Mathf.Clamp01(sliderGestureControl.SliderValue)));
        }

        private void UpdateEventVis(VisProperties settings)
        {
            if (EventVisualization)
            {
                EventVisualization.Init(settings);
            }
        }
    }
}