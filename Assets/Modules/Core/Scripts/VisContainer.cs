// ------------------------------------------------------------------------------------
// <copyright file="VisContainer.cs" company="Technische Universität Dresden">
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
    /// This class stores information about a <see cref="VisAnchor"/>.
    /// </summary>
    public class VisContainer
    {
        /// <summary>
        /// The id of the container.
        /// </summary>
        public int Id = -1;

        /// <summary>
        /// An array of floats representing the quaternion of the orientation of the container.
        /// </summary>
        public float[] Orientation;

        /// <summary>
        /// The parent's id of this container. -1 if this container has no parent.
        /// </summary>
        public int ParentId = -1;

        /// <summary>
        /// An array of floats representing the position vector of the container.
        /// </summary>
        public float[] Position;

        /// <summary>
        /// An array of floats representing the scale vector of the container.
        /// </summary>
        public float[] Scale;
    }
}