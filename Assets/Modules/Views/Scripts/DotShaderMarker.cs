// ------------------------------------------------------------------------------------
// <copyright file="DotShaderMarker.cs" company="Technische Universität Dresden">
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
    /// <summary>
    /// This Unity component represents an object in the shader-based scatter plot.
    /// </summary>
    public class DotShaderMarker : MonoBehaviour
    {
        public Color Color;
        public int Condition;
        public Material Material;
        public MeshFilter MeshFilter;
        public int ObjectId;
        public MeshRenderer Renderer;
        public int Session;
    }
}