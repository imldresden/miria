// ------------------------------------------------------------------------------------
// <copyright file="Axis.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using TMPro;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This unity component is an axis for the coordinate system visualization. In the future, it might be used for other vis as well.
    /// </summary>
    public class Axis : MonoBehaviour
    {
        public Vector3 AxisDirection;
        public Color Color;
        public TextMeshPro Label;
        public string LabelText;
        public float Length;
        public LineRenderer Line;
        public LineRenderer Tip;
        public float TipLength;
        public float TipWidth;
        public float Width;

        /// <summary>
        /// Initializes the axis. Should be called after setting the axis properties.
        /// </summary>
        public void Init()
        {
            Line.positionCount = 2;
            Vector3[] linePositions = { Vector3.zero, AxisDirection.normalized * (Length - TipLength) };
            Line.SetPositions(linePositions);
            Line.startWidth = Width;
            Line.endWidth = Width;
            Line.startColor = Color;
            Line.endColor = Color;

            Tip.positionCount = 2;
            Vector3[] tipPositions = { Line.GetPosition(1), Line.GetPosition(1) + (TipLength * AxisDirection.normalized) };
            Tip.SetPositions(tipPositions);
            Tip.startWidth = TipWidth;
            Tip.endWidth = 0.0f;
            Tip.startColor = Color;
            Tip.endColor = Color;

            Label.gameObject.transform.localPosition = AxisDirection.normalized * Length;
            Label.text = LabelText;
        }
    }
}