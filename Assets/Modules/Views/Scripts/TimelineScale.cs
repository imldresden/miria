// ------------------------------------------------------------------------------------
// <copyright file="TimelineScale.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component draws an axis/scale for the time line.
    /// </summary>
    public class TimelineScale : MonoBehaviour
    {
        public Color Color;
        public float Length;
        public LineRenderer Line;
        public GameObject MaxTimeLabel;
        public float Width;

        private List<LineRenderer> ticks;

        /// <summary>
        /// Initializes the timeline scale with the provided length.
        /// </summary>
        /// <param name="length">The length of the timeline in units.</param>
        public void Init(float length)
        {
            Reset();
            Length = length;
            Width = 0.005f;
            Color = Color.white;

            Line.positionCount = 2;
            Vector3[] linePositions = { Vector3.zero, new Vector3(1, 0, 0) * Length };
            Line.SetPositions(linePositions);
            Line.startWidth = Width;
            Line.endWidth = Width;
            Line.startColor = Color;
            Line.endColor = Color;
            Line.alignment = LineAlignment.TransformZ;

            DrawTicks();
        }

        /// <summary>
        /// Sets the label for the max value.
        /// </summary>
        /// <param name="label">The label string for the max value.</param>
        public void SetMaxLabel(string label)
        {
            MaxTimeLabel.GetComponent<TMPro.TextMeshPro>().text = label;
        }

        private void DrawTicks()
        {
            ticks = new List<LineRenderer>();

            var tickDir = Vector3.Cross(new Vector3(1, 0, 0), Camera.main.transform.forward);

            // Draw ticks
            for (int i = 0; i <= 100; i += 10)
            {
                GameObject tickObject = new GameObject("Tick" + i);
                var tick = tickObject.AddComponent<LineRenderer>();

                if (i % 50 == 0)
                {
                    tick.SetPosition(1, -1 * tickDir * Width * 4f);
                    tick.startWidth = Width / 2;
                    tick.endWidth = Width / 2;
                }
                else
                {
                    tick.SetPosition(1, -1 * tickDir * Width * 2f);
                    tick.startWidth = Width / 3;
                    tick.endWidth = Width / 3;
                }

                tick.material = Line.material;
                tick.useWorldSpace = false;
                tick.startColor = Color;
                tick.endColor = Color;
                tick.alignment = LineAlignment.TransformZ;
                tickObject.transform.parent = transform;
                tickObject.transform.localPosition = new Vector3(1, 0, 0) * Length * i / 100.0f;
                ticks.Add(tick);
            }
        }

        private void Reset()
        {
            if (ticks != null)
            {
                foreach (var tick in ticks)
                {
                    Destroy(tick.gameObject);
                }

                ticks.Clear();
            }
        }
    }
}