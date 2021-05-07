// ------------------------------------------------------------------------------------
// <copyright file="Vis2DMedia.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Core;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Storage;
using Windows.Storage.AccessCache;
#endif

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a 2D media container to display pictures or videos. It implements <see cref="AbstractView"/> and <see cref="IConfigurableVisualization"/>.
    /// </summary>
    public class Vis2DMedia : AbstractView, IConfigurableVisualization
    {
        /// <summary>
        /// The video player component that is used to actually render videos.
        /// </summary>
        public VideoPlayer VideoPlayer;

        /// <summary>
        /// The mesh renderer on which to render.
        /// </summary>
        public MeshRenderer Quad;

        /// <summary>
        /// The prefab for the settings view.
        /// </summary>
        public AbstractSettingsView SettingsViewPrefab;

        private bool isInitialized = false;
        private RenderTexture texture;
        private MediaSource mediaSource;
        private int currentSession, currentCondition;
        private readonly Queue<IEnumerator> coroutines = new Queue<IEnumerator>();

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.Media2D;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => false;

        /// <summary>
        /// Updates the view.
        /// </summary>
        public override void UpdateView()
        {
            // A change without new settings is ignored here
        }

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public override void UpdateView(VisProperties settings)
        {
            Init(settings); // re-initialize with new, updated settings
        }

        /// <summary>
        /// Initializes the visualization with the provided settings.
        /// </summary>
        /// <param name="settings">The settings to use for this visualization.</param>
        public override void Init(VisProperties settings)
        {
            if (isInitialized)
            {
                Reset();
            }

            // no study loaded?
            if (Services.DataManager() == null || Services.DataManager().CurrentStudy == null)
            {
                return; // vis is now reset, we return because there is nothing to load
            }

            Settings = ParseSettings(settings); // parse the settings from the settings object, also makes a deep copy

            VisId = Settings.VisId;
            if (Settings.Sessions != null && Settings.Sessions.Count >= 1)
            {
                currentSession = Settings.Sessions[0];
            }
            else
            {
                currentSession = -1;
            }

            if (Settings.Conditions != null && Settings.Conditions.Count >= 1)
            {
                currentCondition = Settings.Conditions[0];
            }
            else
            {
                currentCondition = -1;
            }

            InitMedia();
            Services.StudyManager().TimelineEventBroadcast.AddListener(TimelineUpdated);
            isInitialized = true;
        }

        /// <summary>
        /// Opens the settings view for this visualization.
        /// </summary>
        public void OpenSettingsUI()
        {
            AbstractSettingsView settingsView = Instantiate(SettingsViewPrefab);
            settingsView.Init(this, false, false);
            settingsView.gameObject.SetActive(true);
        }

        private void InitMedia()
        {
            mediaSource = Services.ContainerManager().GetMediaSourceForContainer(Settings.AnchorId, currentSession, currentCondition);
            if (mediaSource != null && mediaSource.Type == MediaType.VIDEO)
            {
                try
                {
                    _ = GetVideoFile(mediaSource.File);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
                ////PrepareVideo(MediaSource.File);
            }
            else if (mediaSource != null && mediaSource.Type == MediaType.IMAGE)
            {
                // ToDo: prepare image/picture
                byte[] textureData = File.ReadAllBytes(mediaSource.File);
                Texture2D t = new Texture2D(2, 2);
                t.LoadImage(textureData);
                Quad.material.mainTexture = t;
            }
        }

        private async Task GetVideoFile(string file)
        {
#if UNITY_WSA && !UNITY_EDITOR
            // get your file
            //var File = await KnownFolders.CameraRoll.GetFileAsync(file);

            //// generate a token
            //var Path = StorageApplicationPermissions.FutureAccessList.Add(File);

            // resync through a coroutine
            //coroutines.Enqueue(PrepareVideo(Path));
            coroutines.Enqueue(PrepareVideo(file));
#else
            coroutines.Enqueue(PrepareVideo(file));
#endif
        }

        private IEnumerator PrepareVideo(string videoUrl)
        {
            yield return new WaitForEndOfFrame();
            ////GameObject TempVideo = new GameObject();
            ////VideoPlayer TempVideoPlayer = TempVideo.AddComponent<VideoPlayer>();
            ////TempVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            ////TempVideoPlayer.targetTexture = new RenderTexture(1, 1, 0);
            ////TempVideoPlayer.source = UnityEngine.Video.VideoSource.Url;
            ////TempVideoPlayer.url = "file:///" + videoUrl;
            ////TempVideoPlayer.prepareCompleted += PrepareCompleted;
            ////TempVideoPlayer.Prepare();

            // try to get the transform of the current anchor of this vis
            Transform visAnchorTransform = null;
            if (Services.VisManager().ViewContainers.ContainsKey(Settings.AnchorId))
            {
                visAnchorTransform = Services.VisManager().ViewContainers[Settings.AnchorId].transform;
            }
            else
            {
                visAnchorTransform = this.transform; // this does not really help us at all, but at least we won't crash. :>
            }

            // configure actual video player
            texture = CreateRenderTexture(visAnchorTransform, 640, 360); // uses the actual computed video size

            VideoPlayer.source = UnityEngine.Video.VideoSource.Url;
            string url = @"file:///" + videoUrl;
            VideoPlayer.url = url;
            VideoPlayer.targetTexture = texture;
            VideoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            VideoPlayer.playbackSpeed = 1f;
            VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            VideoPlayer.prepareCompleted += PrepareCompleted;
            VideoPlayer.Prepare();
        }

        private void PrepareCompleted(VideoPlayer source)
        {
            VideoPlayer.time = GetVideoTimeForTimestamp(Services.StudyManager().CurrentTimestamp);
            Quad.material.mainTexture = texture;

            ////// try to get the transform of the current anchor of this vis
            ////Transform VisAnchorTransform = null;
            ////if (Services.VisManager().ViewContainers.ContainsKey(Settings.AnchorId))
            ////{
            ////    VisAnchorTransform = Services.VisManager().ViewContainers[Settings.AnchorId].transform;
            ////}
            ////else
            ////{
            ////    VisAnchorTransform = this.transform; // this does not really help us at all, but at least we won't crash. :>
            ////}

            ////// configure actual video player
            ////Texture = CreateRenderTexture(VisAnchorTransform, source.texture.width, source.texture.height); // uses the actual computed video size

            ////VideoPlayer.source = UnityEngine.Video.VideoSource.Url;
            ////VideoPlayer.url = MediaSource.File;
            ////VideoPlayer.targetTexture = Texture;
            ////VideoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            ////VideoPlayer.playbackSpeed = 1f;
            ////VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            ////VideoPlayer.Prepare();
            ////VideoPlayer.time = GetVideoTimeForTimestamp(Services.StudyManager().CurrentTimestamp);
            ////Quad.material.mainTexture = Texture;

            ////// destroy temp video object
            ////Destroy(source.gameObject);
        }

        // Update is called once per frame
        private void Update()
        {
            // This is kind of lame, but quickest hack in the book to get
            // things working when UWP screws up the main thread.
            if (coroutines.Count > 0)
            {
                var action = coroutines.Dequeue();
                StartCoroutine(action);
            }
        }

        /// <summary>
        /// Creates the <c>RenderTexture</c> for this video player based on the size of the parent transform and the maximum height and width.
        /// The resulting texture has the aspect ratio of the base height and width and is not larger than any of the two.
        /// </summary>
        /// <param name="anchorTransform"> the transform, the scale of which will be used to compute the texture</param>
        /// <param name="baseWidth">the maximum width</param>
        /// <param name="baseHeight">the maximum height</param>
        /// <returns>A new <c>RenderTexture</c> with the correct aspect ratio.</returns>
        private RenderTexture CreateRenderTexture(Transform anchorTransform, int baseWidth, int baseHeight)
        {
            int textureWidth, textureHeight;
            if (anchorTransform.localScale.x > anchorTransform.localScale.y)
            {
                textureWidth = baseWidth;
                textureHeight = (int)(baseWidth * (anchorTransform.localScale.y / anchorTransform.localScale.x));
            }
            else
            {
                textureHeight = baseHeight;
                textureWidth = (int)(baseHeight * (anchorTransform.localScale.x / anchorTransform.localScale.y));
            }

            return new RenderTexture(textureWidth, textureHeight, 24);
        }

        private void TimelineUpdated(TimelineState status)
        {
            if (!isInitialized)
            {
                return;
            }

            if (VideoPlayer && VideoPlayer.isPrepared)
            {
                if (VideoPlayer.isPlaying == false && status.TimelineStatus == TimelineStatus.PLAYING)
                {
                    VideoPlayer.Play();
                }
                else if (VideoPlayer.isPlaying == true && status.TimelineStatus == TimelineStatus.PAUSED)
                {
                    VideoPlayer.Pause();
                }

                VideoPlayer.time = GetVideoTimeForTimestamp(status.CurrentTimestamp);
                VideoPlayer.playbackSpeed = status.PlaybackSpeed;
            }
        }

        private double GetVideoTimeForTimestamp(long timestamp)
        {
            if (VideoPlayer && VideoPlayer.isPrepared)
            {
                if (mediaSource.InTime < 0 || mediaSource.InTime > VideoPlayer.length)
                {
                    mediaSource.InTime = 0;
                }

                if (mediaSource.OutTime < mediaSource.InTime || mediaSource.OutTime > VideoPlayer.length)
                {
                    mediaSource.OutTime = (float)VideoPlayer.length;
                }

                long deltaToReferenceTime = timestamp - Math.Max(Services.StudyManager().MinTimestamp, mediaSource.ReferenceTimestamp);
                double deltaInSeconds = deltaToReferenceTime / TimeSpan.TicksPerSecond * 1d;
                return Math.Min(mediaSource.InTime + deltaInSeconds, mediaSource.OutTime);
            }

            return 0;
        }

        private void Reset()
        {
            VideoPlayer.Stop();
            isInitialized = false;
        }
    }
}