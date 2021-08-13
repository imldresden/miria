// ------------------------------------------------------------------------------------
// <copyright file="AbstractDataProvider.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using IMLD.MixedRealityAnalysis.Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// Enum specifying the rotation format.
    /// </summary>
    public enum RotationFormat
    {
        /// <summary>
        /// Euler angles in radians
        /// </summary>
        EULER_RAD,

        /// <summary>
        /// Euler angles in degrees
        /// </summary>
        EULER_DEG,

        /// <summary>
        /// Unity compatible quaternion
        /// </summary>
        QUATERNION,

        /// <summary>
        /// Vector pointing in the look direction of the object
        /// </summary>
        DIRECTION_VECTOR
    }

    /// <summary>
    /// Enum specifying the time format. Currently supported are LONG (timestamp), FLOAT (seconds), STRING (DateTime string).
    /// </summary>
    public enum TimeFormat
    {
        /// <summary>
        /// A timestamp
        /// </summary>
        LONG,

        /// <summary>
        /// Time in seconds
        /// </summary>
        FLOAT,

        /// <summary>
        /// DateTime compatible string
        /// </summary>
        STRING
    }

    /// <summary>
    /// Abstract base class for all data providers.
    /// </summary>
    public abstract class AbstractDataProvider : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the direction of the x-axis. The default is <c>Vector3.right</c>.
        /// </summary>
        public Vector3 AxisDirectionX { get; protected set; } = Vector3.right;

        /// <summary>
        /// Gets or sets the direction of the y-axis. The default is <c>Vector3.up</c>.
        /// </summary>
        public Vector3 AxisDirectionY { get; protected set; } = Vector3.up;

        /// <summary>
        /// Gets or sets the direction of the z-axis. The default is <c>Vector3.forward</c>.
        /// </summary>
        public Vector3 AxisDirectionZ { get; protected set; } = Vector3.forward;

        /// <summary>
        /// Gets or sets a 4x4 matrix representing the transformation of the data's coordinate system into Unity.
        /// </summary>
        public Matrix4x4 AxisTransformationMatrix4x4 { get; protected set; }

        /// <summary>
        /// Gets or sets the currently loaded study.
        /// </summary>
        public StudyData CurrentStudy { get; protected set; }

        /// <summary>
        /// Gets or sets the index of the currently loaded study in the <see cref="StudyList"/>.
        /// </summary>
        public int CurrentStudyIndex { get; protected set; } = -1;

        /// <summary>
        /// Gets or sets a dictionary of the objects or entities in the current study, accessible by their id.
        /// </summary>
        public Dictionary<int, AnalysisObject> DataSets { get; protected set; }

        /// <summary>
        /// Gets or sets a list of <see cref="VisContainer"/>, which gets parsed from the study description.
        /// </summary>
        public List<VisContainer> VisContainers { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data provider is correctly initialized.
        /// </summary>
        public bool IsInitialized { get; protected set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether loading of a study is completed.
        /// </summary>
        public bool IsStudyLoaded { get; protected set; } = false;

        /// <summary>
        /// Gets or sets a list of <see cref="StudyData"/> that represents the parsed meta data of all studies.
        /// </summary>
        public List<StudyData> StudyList { get; protected set; } = new List<StudyData>();

        /// <summary>
        /// Gets the event that is invoked when all study configuration files have been parsed.
        /// </summary>
        public UnityEvent StudyListReady { get; } = new UnityEvent();

        protected virtual void Start()
        {
            if (Services.NetworkManager() != null)
            {
                Services.NetworkManager().ClientConnected += OnClientConnected;
            }
        }

        /// <summary>
        /// Handles the connection of a new client, triggers loading the study.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void OnClientConnected(object sender, NetworkManager.NewClientEventArgs e);

        /// <summary>
        /// Loads the study with the known study id given by <paramref name="index"/>.
        /// </summary>
        /// <param name="index">the index of the study to load.</param>
        /// <returns>Task object</returns>
        public abstract Task LoadStudyAsync(int index);

        /// <summary>
        /// Loads a study from a given xml file.
        /// </summary>
        /// <param name="filepath">The filepath of the study description xml.</param>
        /// <returns>Task object</returns>
        public abstract Task LoadStudyAsync(string filepath);
    }
}