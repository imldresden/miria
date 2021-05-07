// ------------------------------------------------------------------------------------
// <copyright file="TrajectoryMarker.cs" company="Technische Universität Dresden">
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
    /// This Unity component represents a marker indicating the current position of a study object.
    /// </summary>
    public class TrajectoryMarker : MonoBehaviour
    {
        public int ConditionId;
        public int DataSetId;
        public int SessionId;
    }
}