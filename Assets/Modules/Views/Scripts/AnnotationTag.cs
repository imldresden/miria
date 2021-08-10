// ------------------------------------------------------------------------------------
// <copyright file="AnnotationTag.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using IMLD.MixedRealityAnalysis.Core;
using IMLD.MixedRealityAnalysis.Network;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component is used annotation tags, small markers that can be placed and indicate a point of interest to other users.
    /// </summary>
    public class AnnotationTag : MonoBehaviour
    {
        public Color Color;
        public Guid Id;
        public MeshRenderer Renderer;

        /// <summary>
        /// Sends the current position and color over network when the position has been updated.
        /// </summary>
        public void PositionUpdated()
        {
            var message = new MessageUpdateAnnotation(Id, transform.localPosition, Color);
            Services.NetworkManager().SendMessage(message.Pack());
        }

        /// <summary>
        /// Sets the color of this <see cref="AnnotationTag"/>.
        /// </summary>
        /// <param name="color">The new color.</param>
        public void SetColor(Color color)
        {
            Color = color;
            Renderer.material.color = Color;
        }
    }
}