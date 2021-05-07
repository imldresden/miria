// ------------------------------------------------------------------------------------
// <copyright file="StudyManager.cs" company="Technische Universität Dresden">
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
    /// This unity component is used to handle state changes in the study. This includes loading studies, changing the current timestamp, and so on.
    /// </summary>
    public class StudyManager : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="List{int}"/> of the indices of the currently active study conditions.
        /// </summary>
        public List<int> CurrentStudyConditions = new List<int> { 0 };

        /// <summary>
        /// The <see cref="List{int}"/> of the indices of the currently active study sessions.
        /// </summary>
        public List<int> CurrentStudySessions = new List<int> { 0 };

        /// <summary>
        /// The currently active <see cref="TimeFilter"/>.
        /// </summary>
        public TimeFilter CurrentTimeFilter;

        /// <summary>
        /// The current timestamp.
        /// </summary>
        public long CurrentTimestamp;

        /// <summary>
        /// The maximum timestamp in the study data over all currently active sessions, conditions, and study objects.
        /// </summary>
        public long MaxTimestamp;

        /// <summary>
        /// The minimum timestamp in the study data over all currently active sessions, conditions, and study objects.
        /// </summary>
        public long MinTimestamp;

        /// <summary>
        /// Event that is invoked whenever the session filter changes.
        /// </summary>
        public SessionFilterChangedEvent SessionFilterEventBroadcast = new SessionFilterChangedEvent();

        /// <summary>
        /// Event that is invoked whenever the study id changes.
        /// </summary>
        public StudyChangedEvent StudyChangeBroadcast = new StudyChangedEvent();

        /// <summary>
        /// Event that is invoked whenever the time filter changes.
        /// </summary>
        public TimeFilterChangedEvent TimeFilterEventBroadcast = new TimeFilterChangedEvent();

        /// <summary>
        /// Event that is invoked whenever the timeline state changes.
        /// </summary>
        public TimelineStateChangedEvent TimelineEventBroadcast = new TimelineStateChangedEvent();

        /// <summary>
        /// The current status of the timeline.
        /// </summary>
        public TimelineStatus TimelineStatus = TimelineStatus.PAUSED;

        /// <summary>
        /// Gets the current playback speed factor.
        /// </summary>
        public float PlaybackSpeed { get; private set; } = 1f;

        /// <summary>
        /// Loads the study with the provided index.
        /// </summary>
        /// <param name="studyIndex">The index of the study to load.</param>
        public async void LoadStudy(int studyIndex)
        {
            _ = ProgressIndicator.StartProgressIndicator("Loading data...");

            // tell other clients to also load the study
            var command = new MessageLoadStudy(studyIndex);
            Services.NetworkManager().SendMessage(command.Pack());

            // delete all current Visualizations
            Services.VisManager().DeleteAllVisualizations(false);

            // load the actual data
            await Services.DataManager().LoadStudyAsync(studyIndex);
            StudyChangeBroadcast.Invoke(studyIndex);

            // set session filter (also remotely)
            Services.StudyManager().UpdateSessionFilter(new List<int> { 0 }, new List<int> { 0 });

            _ = ProgressIndicator.StopProgressIndicator();
        }

        /// <summary>
        /// Sets the playback speed.
        /// </summary>
        /// <param name="playbackSpeed">The new playback speed factor.</param>
        public void SetPlaybackSpeed(float playbackSpeed)
        {
            PlaybackSpeed = playbackSpeed;
            var message = new MessageUpdateTimeline(new TimelineState(TimelineStatus, CurrentTimestamp, MinTimestamp, MaxTimestamp, PlaybackSpeed));
            Services.NetworkManager().SendMessage(message.Pack());
            TimelineEventBroadcast.Invoke(message.TimelineState);
        }

        /// <summary>
        /// Starts playback.
        /// </summary>
        public void StartPlayback()
        {
            TimelineStatus = TimelineStatus.PLAYING;
            var message = new MessageUpdateTimeline(new TimelineState(TimelineStatus, CurrentTimestamp, MinTimestamp, MaxTimestamp, PlaybackSpeed));
            Services.NetworkManager().SendMessage(message.Pack());
            TimelineEventBroadcast.Invoke(message.TimelineState);
        }

        /// <summary>
        /// Stops/pauses playback.
        /// </summary>
        public void StopPlayback()
        {
            TimelineStatus = TimelineStatus.PAUSED;
            var message = new MessageUpdateTimeline(new TimelineState(TimelineStatus, CurrentTimestamp, MinTimestamp, MaxTimestamp, PlaybackSpeed));
            Services.NetworkManager().SendMessage(message.Pack());
            TimelineEventBroadcast.Invoke(message.TimelineState);
        }

        /// <summary>
        /// Updates the session filter with the provided <see cref="List{int}">Lists</see> of sessions and conditions.
        /// </summary>
        /// <param name="sessions">The indices of the sessions to select.</param>
        /// <param name="conditions">The indices of the conditions to select.</param>
        public void UpdateSessionFilter(List<int> sessions, List<int> conditions)
        {
            CurrentStudyConditions = conditions;
            CurrentStudySessions = sessions;

            UpdateTimestampBounds();

            var message = new MessageUpdateSessionFilter(sessions, conditions);
            Services.NetworkManager().SendMessage(message.Pack());
            SessionFilterEventBroadcast.Invoke();
        }

        /// <summary>
        /// Updates the time filter.
        /// </summary>
        /// <param name="min">A float between 0 and 1 indicating the lower bound of the filter.</param>
        /// <param name="max">A float between 0 and 1 indicating the upper bound of the filter.</param>
        public void UpdateTimeFilter(float min, float max)
        {
            CurrentTimeFilter.MinTime = min;
            CurrentTimeFilter.MaxTime = max;

            UpdateTimestampBounds();

            var message = new MessageUpdateTimeFilter(CurrentTimeFilter);
            Services.NetworkManager().SendMessage(message.Pack());
            TimeFilterEventBroadcast.Invoke(message.TimeFilter);
        }

        /// <summary>
        /// Updates the timeline with a new status and current timestamp.
        /// </summary>
        /// <param name="status">The new <see cref="TimelineStatus"/>.</param>
        /// <param name="currentTimestamp">The new timestamp.</param>
        public void UpdateTimeline(TimelineStatus status, long currentTimestamp)
        {
            TimelineStatus = status;
            CurrentTimestamp = currentTimestamp;
            var message = new MessageUpdateTimeline(new TimelineState(TimelineStatus, CurrentTimestamp, MinTimestamp, MaxTimestamp, PlaybackSpeed));
            Services.NetworkManager().SendMessage(message.Pack());
            TimelineEventBroadcast.Invoke(message.TimelineState);
        }

        private async Task OnLoadStudy(MessageContainer obj)
        {
            Debug.Log("Loading Study");
            Services.NetworkManager().Pause();
            MessageLoadStudy message = MessageLoadStudy.Unpack(obj);
            if (message != null)
            {
                _ = ProgressIndicator.StartProgressIndicator("Loading data...");

                // delete all current Visualizations
                Services.VisManager().DeleteAllVisualizations(false);

                // load the actual data
                await Services.DataManager().LoadStudyAsync(message.StudyIndex);
                StudyChangeBroadcast.Invoke(message.StudyIndex);

                _ = ProgressIndicator.StopProgressIndicator();
            }

            Services.NetworkManager().Unpause();
            Debug.Log("Loading Study - Completed");
        }

        private Task OnSessionFilterChange(MessageContainer obj)
        {
            Debug.Log("Changing Session Filter");
            MessageUpdateSessionFilter message = MessageUpdateSessionFilter.Unpack(obj);
            CurrentStudyConditions = message.Conditions;
            CurrentStudySessions = message.Sessions;

            UpdateTimestampBounds();

            SessionFilterEventBroadcast.Invoke();
            Debug.Log("Changing Session Filter - Completed");
            return Task.CompletedTask;
        }

        private Task OnTimeFilterChange(MessageContainer obj)
        {
            Debug.Log("Changing Filter");
            MessageUpdateTimeFilter message = MessageUpdateTimeFilter.Unpack(obj);
            CurrentTimeFilter = message.TimeFilter;

            UpdateTimestampBounds();

            TimeFilterEventBroadcast.Invoke(message.TimeFilter);
            Debug.Log("Changing Filter - Completed");
            return Task.CompletedTask;
        }

        private Task OnTimelineChange(MessageContainer obj)
        {
            Debug.Log("Changing Timeline");
            MessageUpdateTimeline message = MessageUpdateTimeline.Unpack(obj);
            CurrentTimestamp = message.TimelineState.CurrentTimestamp;
            PlaybackSpeed = message.TimelineState.PlaybackSpeed;
            TimelineStatus = message.TimelineState.TimelineStatus;

            UpdateTimestampBounds();

            TimelineEventBroadcast.Invoke(message.TimelineState);
            Debug.Log("Changing Timeline - Completed");
            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        private void Start()
        {
            CurrentTimeFilter.MinTime = 0;
            CurrentTimeFilter.MaxTime = 1;
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.LOAD_STUDY, OnLoadStudy);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_TIMELINE, OnTimelineChange);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_TIME_FILTER, OnTimeFilterChange);
            Services.NetworkManager().RegisterMessageHandler(MessageContainer.MessageType.UPDATE_SESSION_FILTER, OnSessionFilterChange);
        }

        private void UpdateTimestampBounds()
        {
            MinTimestamp = long.MaxValue;
            MaxTimestamp = long.MinValue;
            foreach (int s in CurrentStudySessions)
            {
                foreach (int c in CurrentStudyConditions)
                {
                    foreach (var dataSet in Services.DataManager().DataSets)
                    {
                        if (dataSet.Value.IsStatic)
                        {
                            continue;
                        }

                        MinTimestamp = Math.Min(dataSet.Value.GetMinTimestamp(s, c), MinTimestamp);
                        MaxTimestamp = Math.Max(dataSet.Value.GetMaxTimestamp(s, c), MaxTimestamp);
                    }
                }
            }

            if (MinTimestamp == long.MinValue)
            {
                MinTimestamp = 0;
            }

            if (MaxTimestamp == long.MinValue)
            {
                MaxTimestamp = 0;
            }

            CurrentTimeFilter.MinTimestamp = (long)(CurrentTimeFilter.MinTime * (MaxTimestamp - MinTimestamp)) + MinTimestamp;
            CurrentTimeFilter.MaxTimestamp = (long)(CurrentTimeFilter.MaxTime * (MaxTimestamp - MinTimestamp)) + MinTimestamp;
        }

        /// <summary>
        /// Invoked, whenever the session filter changes
        /// </summary>
        public class SessionFilterChangedEvent : UnityEvent
        {
        }

        /// <summary>
        /// Invoked, whenever the current study changes
        /// </summary>
        public class StudyChangedEvent : UnityEvent<int>
        {
        }

        /// <summary>
        /// Invoked, whenever the time filter changes
        /// </summary>
        public class TimeFilterChangedEvent : UnityEvent<TimeFilter>
        {
        }

        /// <summary>
        /// Invoked, whenever the timeline position/state changes
        /// </summary>
        public class TimelineStateChangedEvent : UnityEvent<TimelineState>
        {
        }
    }
}

/// <summary>
/// An enum representing the current status of the timeline.
/// </summary>
public enum TimelineStatus
{
    /// <summary>
    /// Playback is running.
    /// </summary>
    PLAYING,

    /// <summary>
    /// Playback is paused.
    /// </summary>
    PAUSED
}

/// <summary>
/// A struct that represents a time filter.
/// </summary>
public struct TimeFilter
{
    /// <summary>
    /// The upper bound of the filter, from 0 to 1.
    /// </summary>
    public float MaxTime;

    /// <summary>
    /// The timestamp of the upper bound of the filter.
    /// </summary>
    public long MaxTimestamp;

    /// <summary>
    /// The lower bound of the filter, from 0 to 1.
    /// </summary>
    public float MinTime;

    /// <summary>
    /// The timestamp of the lower bound of the filter.
    /// </summary>
    public long MinTimestamp;
}

/// <summary>
/// A struct that represents the complete timeline state, including its status, min and max values, current timestamp and playback speed.
/// </summary>
public struct TimelineState
{
    /// <summary>
    /// The current timestamp.
    /// </summary>
    public long CurrentTimestamp;

    /// <summary>
    /// The maximum timestamp.
    /// </summary>
    public long MaxTimestamp;

    /// <summary>
    /// The minimum timestamp.
    /// </summary>
    public long MinTimestamp;

    /// <summary>
    /// The playback speed.
    /// </summary>
    public float PlaybackSpeed;

    /// <summary>
    /// The timeline status.
    /// </summary>
    public TimelineStatus TimelineStatus;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineState"/> struct.
    /// </summary>
    /// <param name="timelineStatus">The timeline status.</param>
    /// <param name="currentTimestamp">The current timestamp.</param>
    /// <param name="minTimestamp">The minimum timestamp.</param>
    /// <param name="maxTimestamp">The maximum timestamp.</param>
    /// <param name="playbackSpeed">The playback speed.</param>
    public TimelineState(TimelineStatus timelineStatus, long currentTimestamp, long minTimestamp, long maxTimestamp, float playbackSpeed)
    {
        TimelineStatus = timelineStatus;
        CurrentTimestamp = currentTimestamp;
        PlaybackSpeed = playbackSpeed;
        MinTimestamp = minTimestamp;
        MaxTimestamp = maxTimestamp;
    }
}