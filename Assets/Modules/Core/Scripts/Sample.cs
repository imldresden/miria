// ------------------------------------------------------------------------------------
// <copyright file="Sample.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// This class represents a single data point for a tracked object in a dataset.
    /// </summary>
    public class Sample
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Sample"/> class.
        /// </summary>
        /// <remarks>This constructor does only set the position, orientation, and scale of the sample.
        /// Make sure to set the other properties as needed.</remarks>
        /// <param name="position">The position of this sample.</param>
        /// <param name="orientation">The orientation of this sample.</param>
        /// <param name="scale">The scale of this sample.</param>
        public Sample(Vector3 position, Quaternion orientation, Vector3 scale)
        {
            Position = position;
            Rotation = orientation;
            Scale = scale;
        }

        /// <summary>
        /// Gets or sets the position of this sample.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the rotation (orientation) of this sample.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Gets or sets the scale of this sample.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the event state of this sample.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of this sample.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the speed of this sample.
        /// </summary>
        public float Speed { get; set; } = 0f;
    }
}