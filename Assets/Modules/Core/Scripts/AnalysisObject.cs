// ------------------------------------------------------------------------------------
// <copyright file="AnalysisObject.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// Enum specifying the object type of an <see cref="AnalysisObject"/>.
    /// </summary>
    public enum ObjectType
    {
        /// <summary>
        /// Object is a tracked person.
        /// </summary>
        USER,

        /// <summary>
        /// Object is a tracked device.
        /// </summary>
        DEVICE,

        /// <summary>
        /// Object is a generic trackable.
        /// </summary>
        TRACKABLE,

        /// <summary>
        /// Object is presenting touches.
        /// </summary>
        TOUCH,

        /// <summary>
        /// The object is a generic physical object.
        /// </summary>
        OBJECT,

        /// <summary>
        /// The object is static.
        /// </summary>
        STATIC,

        /// <summary>
        /// The type of the object is unknown.
        /// </summary>
        UNKNOWN
    }

    /// <summary>
    /// This class describes a (usually) tracked object or person.
    /// </summary>
    public class AnalysisObject
    {
        /// <summary>
        /// The id of this object.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// The name or title of this object. This is used, e.g., for labels in the visualization.
        /// </summary>
        public readonly string Title;

        /// <summary>
        /// The maximum speeds of the samples for all the combinations of conditions and sessions
        /// </summary>
        private readonly float[,] maxSpeeds;

        /// <summary>
        /// A 2D array of all data points of this <c>AnalysisObject</c>.
        /// The array is structured as [Sessions,Conditions] and is used to quickly get access to all samples of a specific session or condition.
        /// </summary>
        private readonly List<Sample>[,] sampleArray;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisObject"/> class.
        /// </summary>
        /// <param name="title">The name or title of this object. This is used, e.g., for labels in the visualization.</param>
        /// <param name="id">The id of this object.</param>
        /// <param name="type">The <see cref="ObjectType"/> of this object.</param>
        /// <param name="parentId">The id of the parent object for this object. Should be -1 if the object has no parent.</param>
        /// <param name="dataSource">The data source for this object. This can be used to differentiate between different sensors or tracking systems.</param>
        /// <param name="unitfactor">The conversion factor between the samples' unit of length and 1m.</param>
        /// <param name="timeformat">The <see cref="TimeFormat"/> for this object</param>
        /// <param name="rotationformat">The <see cref="RotationFormat"/> for this object</param>
        /// <param name="conditions">An ordered <c>List</c> of <c>strings</c> representing the study conditions.</param>
        /// <param name="sessions">An ordered <c>List</c> of <see cref="Session"/> representing the study sessions.</param>
        /// <param name="color">The default <see cref="Color"/> for this object.</param>
        public AnalysisObject(
            string title,
            int id,
            ObjectType type,
            int parentId,
            string dataSource,
            float unitfactor,
            TimeFormat timeformat,
            RotationFormat rotationformat,
            List<string> conditions,
            List<Session> sessions,
            Color color)
        {
            Title = title;
            Id = id;
            ObjectColor = color;
            ParentObjectId = parentId;
            ObjectDataSource = dataSource;
            ObjectType = type;
            UnitConversionFactor = unitfactor;
            RotationFormat = rotationformat;
            TimeFormat = timeformat;

            ConditionCount = conditions.Count;
            SessionCount = sessions.Count;

            sampleArray = new List<Sample>[SessionCount, ConditionCount];
            maxSpeeds = new float[SessionCount, ConditionCount];

            // create dictionaries for conditions
            ConditionToId = new Dictionary<string, int>(conditions.Count);
            IdToCondition = new Dictionary<int, string>(conditions.Count);

            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionToId.Add(conditions[i], i);
                IdToCondition.Add(i, conditions[i]);
            }
        }

        /// <summary>
        /// Gets the average position of all samples.
        /// </summary>
        public Vector3 AveragePosition { get; private set; }

        /// <summary>
        /// Gets the number of conditions.
        /// </summary>
        public int ConditionCount { get; private set; }

        /// <summary>
        /// Gets the <see cref="Dictionary{string, int}"/> mapping conditions to their ids.
        /// </summary>
        public Dictionary<string, int> ConditionToId { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this object has state information connected to it. Should only be set during data import/parsing.
        /// </summary>
        public bool HasStateData { get; set; }

        /// <summary>
        /// Gets the <see cref="Dictionary{int, string}"/> mapping condition ids to their conditions.
        /// </summary>
        public Dictionary<int, string> IdToCondition { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this object is static. Should only be set during data import/parsing.
        /// </summary>
        public bool IsStatic { get; set; } = false;

        /// <summary>
        /// Gets or sets the static local position (i.e., relative to its parent) of this object.
        /// Do not use if this object is not static.
        /// Default is <c>Vector3.zero</c>
        /// </summary>
        public Vector3 LocalPosition { get; set; } = Vector3.zero;

        /// <summary>
        /// Gets or sets the static local rotation (i.e., relative to its parent) of this object.
        /// Do not use if this object is not static.
        /// Default is <c>Quaternion.identity</c>
        /// </summary>
        public Quaternion LocalRotation { get; set; } = Quaternion.identity;

        /// <summary>
        /// Gets or sets the static local scale (i.e., relative to its parent) of this object.
        /// Do not use if this object is not static.
        /// Default is <c>Vector3.one</c>
        /// </summary>
        public Vector3 LocalScale { get; set; } = Vector3.one;

        /// <summary>
        /// Gets the maximum value in x direction of the position data of this object.
        /// </summary>
        public float MaxX { get; private set; }

        /// <summary>
        /// Gets the maximum value in y direction of the position data of this object.
        /// </summary>
        public float MaxY { get; private set; }

        /// <summary>
        /// Gets the maximum value in z direction of the position data of this object.
        /// </summary>
        public float MaxZ { get; private set; }

        /// <summary>
        /// Gets the minimum value in x direction of the position data of this object.
        /// </summary>
        public float MinX { get; private set; }

        /// <summary>
        /// Gets the minimum value in y direction of the position data of this object.
        /// </summary>
        public float MinY { get; private set; }

        /// <summary>
        /// Gets the minimum value in z direction of the position data of this object.
        /// </summary>
        public float MinZ { get; private set; }

        /// <summary>
        /// Gets or sets the default object <see cref="Color"/>.
        /// </summary>
        public Color ObjectColor { get; set; }

        /// <summary>
        /// Gets or sets the data source of this object.
        /// This can be used to differentiate between different sensors or tracking systems.
        /// </summary>
        public string ObjectDataSource { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Mesh"/> for this object.
        /// This is used to show the object in its static position or to indicate its current position on its trajectory.
        /// </summary>
        public Mesh ObjectModel { get; set; }

        /// <summary>
        /// Gets the <see cref="ObjectType"/> of this model.
        /// </summary>
        public ObjectType ObjectType { get; private set; }

        /// <summary>
        /// Gets the id of this object's parent or -1 if it has no parent.
        /// </summary>
        public int ParentObjectId { get; private set; }

        /// <summary>
        /// Gets the <see cref="RotationFormat"/> of this object.
        /// </summary>
        public RotationFormat RotationFormat { get; private set; }

        /// <summary>
        /// Gets the number of study sessions of this object.
        /// </summary>
        public int SessionCount { get; private set; }

        /// <summary>
        /// Gets the <see cref="TimeFormat"/> of this object.
        /// </summary>
        public TimeFormat TimeFormat { get; private set; }

        /// <summary>
        /// Gets the conversion factor between the samples' unit of length and 1m.
        /// </summary>
        public float UnitConversionFactor { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this object's position is static. Default is <see langword="false"/>.
        /// </summary>
        public bool UseStaticPosition { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this object's rotation is static. Default is <see langword="false"/>.
        /// </summary>
        public bool UseStaticRotation { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this object's scale is static. Default is <see langword="false"/>.
        /// </summary>
        public bool UseStaticScale { get; set; } = false;

        /// <summary>
        /// This method returns a filtered list of data points (samples).
        /// This filtering is based on a given session id, a condition id, a starting sample id, a final sample id and a filter.
        /// </summary>
        /// <param name="session">The session id to filter for</param>
        /// <param name="condition">The condition id to filter for</param>
        /// <param name="firstSample">The id of the first sample to include</param>
        /// <param name="lastSample">The id of the last sample to include</param>
        /// <param name="filter">A <see cref="Func{Sample, Sample, bool}"/> used as a filter function.
        /// It takes the current sample and the previous sample as arguments
        /// and returns <see langword="true"/> if the current sample should be included, <see langword="false"/> otherwise.</param>
        /// <returns>An <see cref="IEnumerable{Sample}"/> of the filtered data points (samples) </returns>
        public IEnumerable<Sample> GetFilteredInfoObjects(int session, int condition, int firstSample, int lastSample, Func<Sample, Sample, bool> filter)
        {
            if (firstSample == lastSample)
            {
                yield return sampleArray[session, condition][firstSample];
            }
            else
            {
                Sample previousSample = sampleArray[session, condition][firstSample];
                yield return previousSample;
                for (int i = firstSample + 1; i <= lastSample; i++)
                {
                    Sample currentSample = sampleArray[session, condition][i];
                    if (filter(currentSample, previousSample))
                    {
                        previousSample = currentSample;
                        yield return currentSample;
                    }
                }
            }
        }

        /// <summary>
        /// This method returns a filtered list of data points (samples).
        /// This filtering is based on a given session id, a condition id, a starting sample id, a final sample id and a filter.
        /// </summary>
        /// <param name="session">The session id to filter for</param>
        /// <param name="condition">The condition id to filter for</param>
        /// <param name="filter">A <see cref="Func{Sample, Sample, bool}"/> used as a filter function.
        /// It takes the current sample and the previous sample as arguments
        /// and returns <see langword="true"/> if the current sample should be included, <see langword="false"/> otherwise.</param>
        /// <returns>An <see cref="IEnumerable{Sample}"/> of the filtered data points (samples) </returns>
        public IEnumerable<Sample> GetFilteredInfoObjects(int session, int condition, Func<Sample, Sample, bool> filter)
        {
            return GetFilteredInfoObjects(session, condition, 0, sampleArray[session, condition].Count - 1, filter);
        }

        /// <summary>
        /// Returns the index of the data point for a given timestamp, session and condition.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the lookup</param>
        /// <param name="session">The session to use</param>
        /// <param name="condition">The condition to use</param>
        /// <param name="startIndex">An optional start index for the search.
        /// Use this if you are certain that the result index is larger.</param>
        /// <returns>The index for the data point at or directly before the given timestamp or 0 if this <see cref="AnalysisObject"/> is static.</returns>
        public int GetIndexFromTimestamp(long timestamp, int session, int condition, int startIndex = 0)
        {
            if (IsStatic)
            {
                return 0;
            }

            int lastIndex = sampleArray[session, condition].Count - 1;
            int firstIndex = startIndex;
            int currentIndex = firstIndex + ((lastIndex - firstIndex) / 2);
            int i = 0;
            while (i < 100)
            {
                i++;
                long currentTimestamp = sampleArray[session, condition][currentIndex].Timestamp;

                if (currentIndex == firstIndex || currentIndex == lastIndex)
                {
                    return firstIndex;
                }

                if (currentTimestamp == timestamp)
                {
                    return currentIndex;
                }
                else if (currentTimestamp < timestamp)
                {
                    firstIndex = currentIndex;
                    currentIndex = firstIndex + ((lastIndex - firstIndex) / 2);
                }
                else
                {
                    lastIndex = currentIndex;
                    currentIndex = firstIndex + ((lastIndex - firstIndex) / 2);
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns all samples for a given session and condition.
        /// </summary>
        /// <param name="session">The study session.</param>
        /// <param name="condition">The study condition.</param>
        /// <returns>A <see cref="List{Sample}"/> of all samples for the given session and condition.</returns>
        public List<Sample> GetInfoObjects(int session, int condition)
        {
            return sampleArray[session, condition];
        }

        /// <summary>
        /// Returns the maximum speed for a given session and condition.
        /// </summary>
        /// <param name="session">The study session.</param>
        /// <param name="condition">The study condition.</param>
        /// <returns>The maximum speed over all samples for the given session and condition in m/s.</returns>
        public float GetMaxSpeed(int session, int condition)
        {
            return maxSpeeds[session, condition];
        }

        /// <summary>
        /// Returns the last (i.e., largest) timestamp for a given session and condition
        /// </summary>
        /// <param name="session">The study session.</param>
        /// <param name="condition">The study condition.</param>
        /// <returns>The maximum timestamp.</returns>
        public long GetMaxTimestamp(int session, int condition)
        {
            if (session < 0 || condition < 0)
            {
                return 0;
            }

            if (session >= SessionCount || condition >= ConditionCount)
            {
                return 0;
            }

            if (sampleArray == null || sampleArray[session, condition] == null)
            {
                return 0;
            }

            return sampleArray[session, condition][sampleArray[session, condition].Count - 1].Timestamp;
        }

        /// <summary>
        /// Returns the first (i.e., smallest) timestamp for a given session and condition
        /// </summary>
        /// <param name="session">The study session.</param>
        /// <param name="condition">The study condition.</param>
        /// <returns>The minimum timestamp.</returns>
        public long GetMinTimestamp(int session, int condition)
        {
            if (session < 0 || condition < 0)
            {
                return 0;
            }

            if (session >= SessionCount || condition >= ConditionCount)
            {
                return 0;
            }

            if (sampleArray == null || sampleArray[session, condition] == null)
            {
                return 0;
            }

            return sampleArray[session, condition][0].Timestamp;
        }

        /// <summary>
        /// Recomputes the boundaries in the three axes directions for the samples in this object.
        /// </summary>
        public void RecomputeBounds()
        {
            // Calculate data measures for position
            if (UseStaticPosition)
            {
                MinX = LocalPosition.x;
                MinY = LocalPosition.y;
                MinZ = LocalPosition.z;
                MaxX = MinX;
                MaxY = MinY;
                MaxZ = MinZ;
            }
            else
            {
                MinX = float.MaxValue;
                MinY = float.MaxValue;
                MinZ = float.MaxValue;
                MaxX = float.MinValue;
                MaxY = float.MinValue;
                MaxZ = float.MinValue;

                Vector3 averagePosition = Vector3.zero;
                int sampleCount = 0;
                for (int i = 0; i < sampleArray.GetLength(0); i++)
                {
                    for (int j = 0; j < sampleArray.GetLength(1); j++)
                    {
                        var samples = sampleArray[i, j];
                        foreach (var sample in samples)
                        {
                            Vector3 position = sample.Position;
                            MinX = Mathf.Min(MinX, position.x);
                            MinY = Mathf.Min(MinY, position.y);
                            MinZ = Mathf.Min(MinZ, position.z);
                            MaxX = Mathf.Max(MaxX, position.x);
                            MaxY = Mathf.Max(MaxY, position.y);
                            MaxZ = Mathf.Max(MaxZ, position.z);
                            averagePosition += position;
                            sampleCount++;
                        }
                    }
                }

                AveragePosition = averagePosition / sampleCount;
            }
        }

        /// <summary>
        /// Sets the samples for a given session and condition.
        /// </summary>
        /// <param name="list">The list of samples.</param>
        /// <param name="session">The study session.</param>
        /// <param name="condition">The study condition.</param>
        /// <param name="maxSpeed">The maximum speed as precomputed.</param>
        public void SetSamplesForSessionAndCondition(List<Sample> list, int session, int condition, float maxSpeed)
        {
            sampleArray[session, condition] = list;
            maxSpeeds[session, condition] = Math.Max(maxSpeed, maxSpeeds[session, condition]);
        }
    }
}