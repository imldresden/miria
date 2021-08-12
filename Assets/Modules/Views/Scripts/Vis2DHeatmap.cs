// ------------------------------------------------------------------------------------
// <copyright file="Vis2DHeatmap.cs" company="Technische Universität Dresden">
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
    /// This Unity component is a 2D heatmap visualization. It implements <see cref="AbstractView"/> and <see cref="IConfigurableVisualization"/>.
    /// </summary>
    public class Vis2DHeatmap : AbstractView, IConfigurableVisualization
    {
        /// <summary>
        /// The plane on which to render the heatmap.
        /// </summary>
        public GameObject Plane;

        /// <summary>
        /// The prefab for the settings view.
        /// </summary>
        public AbstractSettingsView SettingsViewPrefab;

        private readonly List<AnalysisObject> dataSets = new List<AnalysisObject>();
        private bool isInitialized = false;
        private int oldTextureWidth, oldTextureHeight;
        private Color[] pixelArray;
        private readonly float[] pixelColorValues = new float[4];
        private Renderer planeRenderer;
        private readonly int resolution = 80;
        private Texture2D texture;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => false;

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.Heatmap2D;

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

            if (Services.DataManager() == null || Services.DataManager().CurrentStudy == null)
            {
                // no study loaded
                return; // vis is now reset, we return because there is nothing to load
            }

            Settings = ParseSettings(settings); // parse the settings from the settings object, also makes a deep copy

            VisId = Settings.VisId;
            foreach (var dataSetIndex in Settings.ObjectIds)
            {
                dataSets.Add((AnalysisObject)Services.DataManager().DataSets[dataSetIndex]);
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
            settingsView.Init(this, false, false);
            settingsView.gameObject.SetActive(true);
        }

        /// <summary>
        /// Updates the view.
        /// </summary>
        public override void UpdateView()
        {
            UpdateView(Settings);
        }

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public override void UpdateView(VisProperties settings)
        {
            Init(settings);
        }

        private Texture2D CreateHeatmapProjectionTexture(Transform anchorTransform, int resolution)
        {
            Transform worldAnchor;
            if (Services.VisManager() != null)
            {
                worldAnchor = Services.VisManager().VisAnchor;
            }
            else
            {
                worldAnchor = this.transform;
            }

            int textureWidth, textureHeight;
            List<Texture2D> entityTextureMaps = new List<Texture2D>();

            if (anchorTransform.localScale.x > anchorTransform.localScale.y)
            {
                textureWidth = resolution;
                textureHeight = (int)(resolution * (anchorTransform.localScale.y / anchorTransform.localScale.x));
            }
            else
            {
                textureHeight = resolution;
                textureWidth = (int)(resolution * (anchorTransform.localScale.x / anchorTransform.localScale.y));
            }

            if (texture == null || textureWidth != texture.width || textureHeight != texture.height)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }

                texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            if (pixelArray == null || textureWidth != oldTextureWidth || textureHeight != oldTextureHeight)
            {
                pixelArray = new Color[textureWidth * textureHeight];
                oldTextureWidth = textureWidth;
                oldTextureHeight = textureHeight;
            }

            // iterate over all AnalysisDataSets to compute max/min size of the attributes
            foreach (var dataSet in dataSets)
            {
                // only non-static datasets contribute to heatmaps
                if (!dataSet.IsStatic)
                {
                    int[,] factorMap = new int[textureWidth, textureHeight];
                    float maxFactor = 0;
                    int stepSize = 1;
                    Color currentEntityColor = dataSet.ObjectColor;
                    Texture2D tmpTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);

                    // for all sessions that we want to visualize...
                    for (int s = 0; s < Settings.Sessions.Count; s++)
                    {
                        // for all conditions that we want to visualize...
                        for (int c = 0; c < Settings.Conditions.Count; c++)
                        {
                            List<Sample> infoObjects = dataSet.GetInfoObjects(Settings.Sessions[s], Settings.Conditions[c]);
                            stepSize = 1 + (infoObjects.Count / 1000);

                            // iterate over info objects
                            for (int i = 0; i < infoObjects.Count; i += stepSize)
                            {
                                Sample o = infoObjects[i];

                                // compute projected position on anchor plane
                                Vector3 projectedPosition = ProjectToAnchorPlane(o.Position, worldAnchor, anchorTransform);

                                int x = Mathf.FloorToInt((projectedPosition.x + 0.5f) * textureWidth);
                                int y = Mathf.FloorToInt((projectedPosition.y + 0.5f) * textureHeight);
                                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                                {
                                    maxFactor = Math.Max(++factorMap[x, y], maxFactor);
                                }
                            }
                        }
                    }

                    maxFactor = Math.Max(1, maxFactor / (50 * stepSize));
                    Color newColor;

                    // set alpha for each pixel
                    for (int i = 0; i < textureWidth; i++)
                    {
                        for (int j = 0; j < textureHeight; j++)
                        {
                            // calculate transparency based on factor
                            if (factorMap[i, j] > 0)
                            {
                                newColor = currentEntityColor * ((float)factorMap[i, j] / (float)maxFactor);
                                newColor.a = (float)factorMap[i, j] / (float)maxFactor;
                            }
                            else
                            {
                                newColor = Color.clear;
                            }

                            pixelArray[(textureWidth * j) + i] = newColor;
                        }
                    }

                    tmpTexture.SetPixels(pixelArray);
                    entityTextureMaps.Add(tmpTexture);
                }
            }

            // set final texture values
            Color finalColor;
            for (int i = 0; i < textureWidth; i++)
            {
                for (int j = 0; j < textureHeight; j++)
                {
                    pixelColorValues[0] = 0f;
                    pixelColorValues[1] = 0f;
                    pixelColorValues[2] = 0f;
                    pixelColorValues[3] = 0f;

                    // calculate color based on all temporary textures
                    foreach (Texture2D tempTexture in entityTextureMaps)
                    {
                        Color currentTexturePixelColor = tempTexture.GetPixel(i, j);

                        if (currentTexturePixelColor != Color.clear)
                        {
                            pixelColorValues[0] += currentTexturePixelColor.r;
                            pixelColorValues[1] += currentTexturePixelColor.g;
                            pixelColorValues[2] += currentTexturePixelColor.b;
                            pixelColorValues[3] += currentTexturePixelColor.a;
                        }
                    }

                    finalColor.r = pixelColorValues[0];
                    finalColor.g = pixelColorValues[1];
                    finalColor.b = pixelColorValues[2];
                    finalColor.a = pixelColorValues[3];

                    pixelArray[(textureWidth * j) + i] = finalColor;
                }
            }

            // blur horizontally
            for (int i = 1; i < textureWidth - 1; i++)
            {
                for (int j = 1; j < textureHeight - 1; j++)
                {
                    finalColor = (pixelArray[(textureWidth * j) + (i - 1)] * 0.27901f) + (pixelArray[(textureWidth * j) + i] * 0.44198f) + (pixelArray[(textureWidth * j) + (i + 1)] * 0.27901f);
                    pixelArray[(textureWidth * j) + i] = finalColor;
                }
            }

            // blur vertically
            for (int i = 1; i < textureWidth - 1; i++)
            {
                for (int j = 1; j < textureHeight - 1; j++)
                {
                    finalColor = (pixelArray[(textureWidth * (j - 1)) + i] * 0.27901f) + (pixelArray[(textureWidth * j) + i] * 0.44198f) + (pixelArray[(textureWidth * (j + 1)) + i] * 0.27901f);
                    pixelArray[(textureWidth * j) + i] = finalColor;
                }
            }

            texture.SetPixels(pixelArray);

            // clean up
            foreach (Texture2D tempTexture in entityTextureMaps)
            {
                Destroy(tempTexture);
            }

            texture.Apply();
            return texture;
        }

        private void DrawGraph()
        {
            // get extends from current vis anchor
            Transform visAnchorTransform;

            // try to get the transform of the current anchor of this vis
            if (Services.VisManager().ViewContainers.ContainsKey(Settings.AnchorId))
            {
                visAnchorTransform = Services.VisManager().ViewContainers[Settings.AnchorId].transform;
            }
            else
            {
                visAnchorTransform = this.transform; // this does not really help us at all, but at least we won't crash. :>
            }

            planeRenderer = Plane.GetComponentInChildren<Renderer>();

            texture = CreateHeatmapProjectionTexture(visAnchorTransform, resolution);
            planeRenderer.material.mainTexture = texture;
            texture.Apply();
        }

        private Vector2 ProjectToAnchorPlane(Vector3 rawPosition, Transform worldAnchor, Transform transform)
        {
            Vector3 position = worldAnchor.TransformPoint(rawPosition);
            Vector3 projectedVector = position - Vector3.Project(position - transform.position, transform.forward.normalized);
            Vector3 relativeProjectedVector = transform.InverseTransformPoint(projectedVector);
            return relativeProjectedVector;
        }

        private void Reset()
        {
            isInitialized = false;

            dataSets.Clear();
        }
    }
}