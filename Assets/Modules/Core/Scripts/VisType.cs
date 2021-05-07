// ------------------------------------------------------------------------------------
// <copyright file="VisType.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This <see langword="enum"/> represents the different type of visualizations.
    /// </summary>
    public enum VisType
    {
        /// <summary>
        /// A 3D trajectory visualization.
        /// </summary>
        Trajectory3D,

        /// <summary>
        /// A 3D trail visualization.
        /// </summary>
        Trail3D,

        /// <summary>
        /// 3D models to represent a tracked object or virtual scene objects.
        /// </summary>
        Model3D,

        /// <summary>
        /// A 3D coordinate system representation.
        /// </summary>
        CoordinateSystem3D,

        /// <summary>
        /// A timeline controller.
        /// </summary>
        TimelineControl,

        /// <summary>
        /// A 2D heatmap or density map.
        /// </summary>
        Heatmap2D,

        /// <summary>
        /// A 2D point plot of current object locations.
        /// </summary>
        Location2D,

        /// <summary>
        /// A 2D scatterplot.
        /// </summary>
        Scatterplot2D,

        /// <summary>
        /// A 2D media view, showing a video or still picture.
        /// </summary>
        Media2D,

        /// <summary>
        /// A 2D event timeline/scarf plot.
        /// </summary>
        Event2D
    }
}