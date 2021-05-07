// ------------------------------------------------------------------------------------
// <copyright file="Vis2DEvents.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Collections.Generic;
using IMLD.MixedRealityAnalysis.Core;
using TMPro;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a 2D event visualization. It implements <see cref="AbstractView"/> and <see cref="IConfigurableVisualization"/>.
    /// </summary>
    public class Vis2DEvents : AbstractView, IConfigurableVisualization
    {
        /// <summary>
        /// The height of this event visualization.
        /// </summary>
        public float Height;

        /// <summary>
        /// The material for the labels.
        /// </summary>
        public Material LabelMaterial;

        /// <summary>
        /// The prefab for the labels.
        /// </summary>
        public TextMeshPro LabelPrefab;

        /// <summary>
        /// The prefab for the settings view.
        /// </summary>
        public AbstractSettingsView SettingsViewPrefab;

        /// <summary>
        /// The width of this event visualization.
        /// </summary>
        public float Width;

        /// <summary>
        /// A list of the currently used analysis objects.
        /// </summary>
        protected List<AnalysisObject> dataSets = new List<AnalysisObject>();

        //private long currentTimeFilterMax = long.MinValue;
        //private long currentTimeFilterMin = long.MaxValue;
        private bool isInitialized = false;
        private long maxTimestamp = long.MinValue;
        private long minTimestamp = long.MaxValue;
        private List<GameObject> quads;
        private Dictionary<string, Color> stateColorMap;
        private Texture2D texture;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => false;

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.Event2D;

        /// <summary>
        /// Initializes the visualization with the provided settings.
        /// </summary>
        /// <param name="settings">The settings to use for this visualization.</param>
        public override void Init(VisProperties settings)
        {
            if (isInitialized)
            {
                Reset(); // reset if we are already initialized
            }

            if (Services.DataManager() == null || Services.DataManager().CurrentStudy == null)
            {
                // no study loaded
                return; // vis is now reset, we return because there is nothing to load
            }

            Settings = ParseSettings(settings); // parse the settings from the settings object, also makes a deep copy

            VisId = Settings.VisId;
            foreach (var dataSetIndex in Settings.ObjectIds)
            {
                if (Services.DataManager().DataSets[dataSetIndex].HasStateData)
                {
                    dataSets.Add(Services.DataManager().DataSets[dataSetIndex]);
                }
            }

            // get filter min/max
            if (Services.StudyManager() != null)
            {
                minTimestamp = Services.StudyManager().MinTimestamp;
                maxTimestamp = Services.StudyManager().MaxTimestamp;
                //currentTimeFilterMin = Services.StudyManager().CurrentTimeFilter.MinTimestamp;
                //currentTimeFilterMax = Services.StudyManager().CurrentTimeFilter.MaxTimestamp;

                Services.StudyManager().TimeFilterEventBroadcast.AddListener(TimeFilterUpdated); // set listener so we can get notified about future updates
            }

            DrawGraph();

            isInitialized = true;
        }

        /// <summary>
        /// Opens the settings user interface for the visualization.
        /// </summary>
        public void OpenSettingsUI()
        {
            AbstractSettingsView settingsView = Instantiate(SettingsViewPrefab);
            settingsView.Init(this, false, true);
            settingsView.gameObject.SetActive(true);
        }

        /// <summary>
        /// Updates the view.
        /// </summary>
        public override void UpdateView()
        {
            Init(Settings); // re-initialize
        }

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public override void UpdateView(VisProperties settings)
        {
            Init(settings); // re-initialize
        }

        private int ComputePositionInTexture(int width, long currentTimestamp)
        {
            return (int)((currentTimestamp - minTimestamp) / (float)(maxTimestamp - minTimestamp) * width);
        }

        private void CreateLabel(int index)
        {
            float height = (Height / dataSets.Count) * 0.95f;

            TextMeshPro label = Instantiate(LabelPrefab, Anchor);
            label.transform.Translate(new Vector3(0f, 1.05f * height * index, 0f));
            label.text = dataSets[index].Title;
            MeshRenderer renderer = label.gameObject.transform.GetChild(0).GetComponentInChildren<MeshRenderer>();
            if (renderer && LabelMaterial)
            {
                Material material = new Material(LabelMaterial)
                {
                    color = dataSets[index].ObjectColor
                };
                renderer.material = material;
            }
        }

        private void CreateMesh(int index)
        {
            float width = Width;
            float height = (Height / dataSets.Count) * 0.95f;
            GameObject go = new GameObject();
            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
            Material material = new Material(Shader.Find("Unlit/Transparent"))
            {
                mainTexture = texture
            };
            meshRenderer.sharedMaterial = material;

            MeshFilter meshFilter = go.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(0, height, 0),
            new Vector3(width, height, 0)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
            // lower left triangle
            0, 2, 1,

            // upper right triangle
            2, 3, 1
            };

            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
            };
            mesh.normals = normals;

            float minSpeed = Mathf.Clamp((float)index / texture.height, 0, 1);
            float maxSpeed = Mathf.Clamp((float)(index + 1) / texture.height, 0, 1);

            Vector2[] uv = new Vector2[4]
            {
            new Vector2(0, minSpeed),
            new Vector2(1, minSpeed),
            new Vector2(0, maxSpeed),
            new Vector2(1, maxSpeed)
            };
            mesh.uv = uv;

            meshFilter.mesh = mesh;

            go.transform.SetParent(Anchor, false);
            go.transform.Translate(new Vector3(0f, 1.05f * height * index, 0f));
            go.transform.localScale = Vector3.one;
        }

        private void CreateTexture()
        {
            int textureWidth = 800;
            int textureHeight = dataSets.Count * Settings.Sessions.Count * Settings.Conditions.Count;
            texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] colorValues = new Color[textureWidth * textureHeight];
            for (int i = 0; i < colorValues.Length; i++)
            {
                colorValues[i] = Color.clear;
            }

            int posX, posY;
            Color currentColor;
            stateColorMap = new Dictionary<string, Color>();
            for (int d = 0; d < dataSets.Count; d++)
            {
                // for all data sets
                posY = d;
                var infoObjects = dataSets[d].GetInfoObjects(Settings.Sessions[0], Settings.Conditions[0]); // get data for first (only!) session/condition
                for (int i = 0; i < infoObjects.Count; i++)
                {
                    Sample currentSample = infoObjects[i];
                    if (currentSample.State != null && currentSample.State.Length > 0)
                    {
                        if (!stateColorMap.ContainsKey(currentSample.State))
                        {
                            stateColorMap.Add(currentSample.State, GenerateNextColor());
                        }

                        // get color
                        currentColor = stateColorMap[currentSample.State];

                        // compute desired x position
                        posX = ComputePositionInTexture(textureWidth, currentSample.Timestamp);

                        // get alternative position if necessary
                        while (colorValues[(posY * textureWidth) + posX] != Color.clear && colorValues[(posY * textureWidth) + posX] != currentColor)
                        {
                            posX++;
                            if (posX >= textureWidth)
                            {
                                break;
                            }
                        }

                        colorValues[(posY * textureWidth) + posX] = currentColor;
                    }
                }
            }

            texture.SetPixels(colorValues);
            texture.filterMode = FilterMode.Point;

            texture.Apply();
        }

        private void DrawGraph()
        {
            quads = new List<GameObject>();
            if (dataSets.Count == 0)
            {
                return;
            }

            CreateTexture();
            for (int d = 0; d < dataSets.Count; d++)
            {
                CreateMesh(d);
                CreateLabel(d);
            }
        }

        private Color GenerateNextColor()
        {
            if (stateColorMap == null)
            {
                return Color.white;
            }

            switch (stateColorMap.Count)
            {
                case 0:
                    return new Color(0.4f, 0.76f, 0.65f, 1f);

                case 1:
                    return new Color(0.99f, 0.55f, 0.38f, 1f);

                case 2:
                    return new Color(0.55f, 0.63f, 0.80f, 1f);

                case 3:
                    return new Color(0.91f, 0.54f, 0.76f, 1f);

                case 4:
                    return new Color(0.65f, 0.85f, 0.33f, 1f);

                default:
                    return Random.ColorHSV(0f, 1f, 0.1f, 1f, 0.5f, 1f);
            }
        }

        private void Reset()
        {
            Destroy(texture);

            if (quads != null)
            {
                foreach (var quad in quads)
                {
                    Destroy(quad);
                }

                quads.Clear();
            }

            ////Settings = new VisSettings(Settings.VisId, Settings.VisType, Settings.AnchorId, null, null, null, null, Settings.Position, Settings.Orientation);

            if (stateColorMap != null)
            {
                stateColorMap.Clear();
            }

            if (dataSets != null)
            {
                dataSets.Clear();
            }

            minTimestamp = long.MaxValue;
            maxTimestamp = long.MinValue;

            isInitialized = false;
        }

        private void TimeFilterUpdated(TimeFilter timeFilter)
        {
            // set filtered min and max, based on filter value [0,1] and global min/max
            //currentTimeFilterMin = (long)(timeFilter.MinTime * (maxTimestamp - minTimestamp)) + minTimestamp;
            //currentTimeFilterMax = (long)(timeFilter.MaxTime * (maxTimestamp - minTimestamp)) + minTimestamp;
        }
    }
}