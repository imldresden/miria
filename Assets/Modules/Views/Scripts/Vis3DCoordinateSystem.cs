// ------------------------------------------------------------------------------------
// <copyright file="Vis3DCoordinateSystem.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Collections.Generic;
using IMLD.MixedRealityAnalysis.Core;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is a 3D coordinate system. It implements <see cref="AbstractView"/>.
    /// </summary>
    public class Vis3DCoordinateSystem : AbstractView
    {
        /// <summary>
        /// The prefab for a coordinate axis.
        /// </summary>
        public Axis AxisPrefab;

        private readonly List<Axis> axes = new List<Axis>();
        private bool isInitialized = false;

        /// <summary>
        /// Gets a value indicating whether this view is three-dimensional.
        /// </summary>
        public override bool Is3D => true;

        /// <summary>
        /// Gets the type of the visualization.
        /// </summary>
        public override VisType VisType => VisType.CoordinateSystem3D;

        /// <summary>
        /// Initializes the view with the provided settings.
        /// </summary>
        /// <param name="settings">The settings to use for this view.</param>
        public override void Init(VisProperties settings)
        {
            Init();
        }

        /// <summary>
        /// Updates the view.
        /// </summary>
        public override void UpdateView()
        {
            Init();
        }

        /// <summary>
        /// Updates the view with the provided settings.
        /// </summary>
        /// <param name="settings">The new settings for the view.</param>
        public override void UpdateView(VisProperties settings)
        {
            Init();
        }

        private void Init()
        {
            if (isInitialized)
            {
                Reset();
            }

            Axis axis;

            // get current axis directions
            Vector3 axisX = Services.DataManager().AxisDirectionX;
            Vector3 axisY = Services.DataManager().AxisDirectionY;
            Vector3 axisZ = Services.DataManager().AxisDirectionZ;

            // x
            axis = Instantiate<Axis>(AxisPrefab);
            axis.AxisDirection = axisX;
            axis.Color = Color.red;
            axis.transform.SetParent(Anchor, false);
            axis.LabelText = "X";
            axis.Init();
            axes.Add(axis);

            // y
            axis = Instantiate<Axis>(AxisPrefab);
            axis.AxisDirection = axisY;
            axis.Color = Color.green;
            axis.transform.SetParent(Anchor, false);
            axis.LabelText = "Y";
            axis.Init();
            axes.Add(axis);

            // z
            axis = Instantiate<Axis>(AxisPrefab);
            axis.AxisDirection = axisZ;
            axis.Color = Color.blue;
            axis.transform.SetParent(Anchor, false);
            axis.LabelText = "Z";
            axis.Init();
            axes.Add(axis);

            isInitialized = true;
        }

        private void Reset()
        {
            if (axes != null)
            {
                foreach (var obj in axes)
                {
                    if (obj)
                    {
                        Destroy(obj.gameObject);
                    }
                }

                axes.Clear();
            }

            isInitialized = false;
        }
    }
}