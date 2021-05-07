// ------------------------------------------------------------------------------------
// <copyright file="StudyData.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Xml.Serialization;

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// Represents the data about a data set.
    /// This is part of the model for the xml config files used by MIRIA.
    /// </summary>
    [XmlRoot("AnalysisXml")]
    public class StudyData
    {
        /// <summary>
        /// Gets or sets the <see cref="List{VisAnchor}"/> of anchors.
        /// </summary>
        [XmlArray("anchors")]
        [XmlArrayItem("anchor")]
        public List<VisAnchor> Anchors { get; set; }

        /// <summary>
        /// Gets or sets the direction of the x-axis. The default is "right".
        /// </summary>
        [XmlAttribute("axis_direction_x")]
        public string AxisDirectionX { get; set; } = "right";

        /// <summary>
        /// Gets or sets the direction of the y-axis. The default is "up".
        /// </summary>
        [XmlAttribute("axis_direction_y")]
        public string AxisDirectionY { get; set; } = "up";

        /// <summary>
        /// Gets or sets the direction of the z-axis. The default is "forward".
        /// </summary>
        [XmlAttribute("axis_direction_z")]
        public string AxisDirectionZ { get; set; } = "forward";

        /// <summary>
        /// Gets or sets the list of study conditions (e.g., techniques).
        /// </summary>
        [XmlArray("conditions")]
        [XmlArrayItem("condition")]
        public List<string> Conditions { get; set; }

        /// <summary>
        /// Gets or sets the id of this data set.
        /// </summary>
        /// <remarks>This field is not serialized.</remarks>
        [XmlIgnore]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the list of media sources.
        /// </summary>
        [XmlArray("mediasources")]
        [XmlArrayItem("mediasource")]
        public List<MediaSource> MediaSources { get; set; }

        /// <summary>
        /// Gets or sets the list of objects.
        /// </summary>
        [XmlArray("objects")]
        [XmlArrayItem("object")]
        public List<StudyObject> Objects { get; set; }

        /// <summary>
        /// Gets or sets the list of object sources.
        /// </summary>
        [XmlArray("objectsources")]
        [XmlArrayItem("objectsource")]
        public List<ObjectSource> ObjectSources { get; set; }

        /// <summary>
        /// Gets or sets the list of study sessions.
        /// </summary>
        [XmlArray("sessions")]
        [XmlArrayItem("session")]
        public List<Session> Sessions { get; set; }

        /// <summary>
        /// Gets or sets the name of the study.
        /// </summary>
        [XmlAttribute("name")]
        public string StudyName { get; set; }
    }
}

/// <summary>
/// Enum specifying the media type.
/// </summary>
public enum MediaType
{
    /// <summary>
    /// The medium is a video.
    /// </summary>
    [XmlEnum("video")]
    VIDEO = 0,

    /// <summary>
    /// The medium is an image.
    /// </summary>
    [XmlEnum("image")]
    IMAGE = 1
}

/// <summary>
/// Represents a media object that can be attached to anchors.
/// </summary>
[XmlType("mediasource")]
public class MediaSource
{
    /// <summary>
    /// Gets or sets the id of the anchor/vis plane that this video source is providing data for.
    /// </summary>
    /// <remarks>Use -1 if this video is not related to a specific plane.</remarks>
    [XmlAttribute("anchor_id")]
    public int AnchorId { get; set; }

    /// <summary>
    /// Gets or sets the id of the condition that this video source is providing data for.
    /// </summary>
    [XmlAttribute("condition_id")]
    public int ConditionId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the file name for this media source
    /// </summary>
    [XmlAttribute("file")]
    public string File { get; set; }

    /// <summary>
    /// Gets or sets the time in seconds from the start of the video that marks the first frame that should be used.
    /// </summary>
    /// <remarks>Use this in combination with <c>OutTime</c> to only show a part of a longer video, for example if the video file contains more than one session or condition</remarks>
    [XmlAttribute("in_time")]
    public float InTime { get; set; } = -1;

    /// <summary>
    /// Gets or sets the time in seconds from the start of the video that marks the last frame that should be used.
    /// </summary>
    [XmlAttribute("out_time")]
    public float OutTime { get; set; } = -1;

    /// <summary>
    /// Gets or sets the timestamp in the corresponding data that this video should be synchronized to.
    /// </summary>
    [XmlAttribute("reference_timestamp")]
    public long ReferenceTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the id of the session that this video source is providing data for.
    /// </summary>
    [XmlAttribute("session_id")]
    public int SessionId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the file name for this media source
    /// </summary>
    [XmlAttribute("type")]
    public MediaType Type { get; set; }
}

/// <summary>
/// This defines for a combination of object, study session, and study condition which csv file to read.
/// In case several conditions or sessions are logged in the same file, the filter properties can be used.
/// </summary>
[XmlType("objectsource")]
public class ObjectSource
{
    /// <summary>
    /// Gets or sets the <c>string</c> that is used to filter the condition data.
    /// </summary>
    /// <remarks>
    /// Use this attribute to filter the data from a file that contains more than one condition.
    /// Only the rows where the <c>ConditionFilterColumn</c> contains this value will be used.
    /// </remarks>
    [XmlAttribute("condition_filter")]
    public string ConditionFilter { get; set; }

    /// <summary>
    /// Gets or sets the name of the column that is used to filter the condition data.
    /// </summary>
    /// <remarks>
    /// Use this attribute to filter the data from a file that contains more than one condition.
    /// Only the rows where this column contains the value of <c>ConditionFilter</c> will be used.
    /// </remarks>
    [XmlAttribute("condition_filter_column")]
    public string ConditionFilterColumn { get; set; }

    /// <summary>
    /// Gets or sets the id of the condition that this object source is providing data for.
    /// </summary>
    [XmlAttribute("condition_id")]
    public int ConditionId { get; set; }

    /// <summary>
    /// Gets or sets the file name for this object source
    /// </summary>
    [XmlAttribute("file")]
    public string File { get; set; }

    /// <summary>
    /// Gets or sets the id of the analysis object that this object source is providing data for.
    /// </summary>
    [XmlAttribute("object_id")]
    public int ObjectId { get; set; }

    /// <summary>
    /// Gets or sets the <c>string</c> that is used to filter the session data.
    /// </summary>
    /// <remarks>
    /// Use this attribute to filter the data from a file that contains more than one session.
    /// Only the rows where the <c>SessionFilterColumn</c> contains this value will be used.
    /// </remarks>
    [XmlAttribute("session_filter")]
    public string SessionFilter { get; set; }

    /// <summary>
    /// Gets or sets the name of the column that is used to filter the session data.
    /// </summary>
    /// <remarks>
    /// Use this attribute to filter the data from a file that contains more than one session.
    /// Only the rows where this column contains the value of <c>SessionFilter</c> will be used.
    /// </remarks>
    [XmlAttribute("session_filter_column")]
    public string SessionFilterColumn { get; set; }

    /// <summary>
    /// Gets or sets the id of the session that this object source is providing data for.
    /// </summary>
    [XmlAttribute("session_id")]
    public int SessionId { get; set; }
}

/// <summary>
/// Represents a study session.
/// </summary>
[XmlType("session")]
public class Session
{
    /// <summary>
    /// Gets or sets the id of the session.
    /// </summary>
    [XmlAttribute("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the session.
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; }
}

/// <summary>
/// Represents an object or entity in the study. Each tracked object has a set of attributes defined here.
/// </summary>
[XmlType("object")]
public class StudyObject
{
    /// <summary>
    /// Gets or sets the hue of this object's default color in the HSV color space.
    /// </summary>
    [XmlAttribute("hue")]
    public float ColorHue { get; set; }

    /// <summary>
    /// Gets or sets the saturation of this object's default color in the HSV color space.
    /// </summary>
    [XmlAttribute("saturation")]
    public float ColorSaturation { get; set; }

    /// <summary>
    /// Gets or sets the value of this object's default color in the HSV color space.
    /// </summary>
    [XmlAttribute("value")]
    public float ColorValue { get; set; }

    /// <summary>
    /// Gets or sets the id of this object.
    /// </summary>
    [XmlAttribute("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this object is static (i.e., not changing over time during the study).
    /// </summary>
    [XmlAttribute("static")]
    public bool IsStatic { get; set; }

    /// <summary>
    /// Gets or sets the model file used for this object.
    /// </summary>
    [XmlElement("model")]
    public string ModelFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of this object.
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the string representing the type of this object. Should be one defined in <see cref="ObjectType"/>.
    /// </summary>
    [XmlAttribute("type")]
    public string ObjectType { get; set; }

    /// <summary>
    /// Gets or sets the id of this objects parent.
    /// </summary>
    [XmlAttribute("parent")]
    public int ParentId { get; set; }

    /// <summary>
    /// Gets or sets the object's source column for the x position or a static value in {}.
    /// </summary>
    [XmlElement("transform_position_x")]
    public string PositionXSource { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the object's source column for the y position or a static value in {}.
    /// </summary>
    [XmlElement("transform_position_y")]
    public string PositionYSource { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the object's source column for the z position or a static value in {}.
    /// </summary>
    [XmlElement("transform_position_z")]
    public string PositionZSource { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the string representing the rotation format of this object. Should be one of <see cref="RotationFormat"/>.
    /// </summary>
    [XmlAttribute("rotation_format")]
    public string RotationFormat { get; set; }

    /// <summary>
    /// Gets or sets the object's source column for the w component of the rotation or a static value in {}.
    /// </summary>
    [XmlElement("transform_rotation_w")]
    public string RotationWSource { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the object's source column for the x component of the rotation or a static value in {}.
    /// </summary>
    [XmlElement("transform_rotation_x")]
    public string RotationXSource { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the object's source column for the y component of the rotation or a static value in {}.
    /// </summary>
    [XmlElement("transform_rotation_y")]
    public string RotationYSource { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the object's source column for the z component of the rotation or a static value in {}.
    /// </summary>
    [XmlElement("transform_rotation_z")]
    public string RotationZSource { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the object's source column for the x component of the scale or a static value in {}.
    /// </summary>
    [XmlElement("transform_scale_x")]
    public string ScaleXSource { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the object's source column for the y component of the scale or a static value in {}.
    /// </summary>
    [XmlElement("transform_scale_y")]
    public string ScaleYSource { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the object's source column for the z component of the scale or a static value in {}.
    /// </summary>
    [XmlElement("transform_scale_z")]
    public string ScaleZSource { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the string describing the source of this object's data, e.g., the type of tracking system used.
    /// </summary>
    [XmlAttribute("data_source")]
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the object's source column for its event state.
    /// </summary>
    [XmlElement("state")]
    public string StateSource { get; set; }

    /// <summary>
    /// Gets or sets the format of the timestamp for this object. Should be one of <see cref="TimeFormat"/>.
    /// </summary>
    [XmlAttribute("time_format")]
    public string TimeFormat { get; set; }

    /// <summary>
    /// Gets or sets the object's source column for its timestamps.
    /// </summary>
    [XmlElement("timestamp")]
    public string TimestampSource { get; set; }

    /// <summary>
    /// Gets or sets the units used by the values of this object. Should be "m", "cm", or "mm".
    /// </summary>
    [XmlAttribute("units")]
    public string Units { get; set; }
}

/// <summary>
/// Represents an anchor for 2D visualizations or media views.
/// </summary>
[XmlType("anchor")]
public class VisAnchor
{
    /// <summary>
    /// Gets or sets the id of the anchor.
    /// </summary>
    [XmlAttribute("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent id of the anchor. Set this to the id of one of the <see cref="StudyObject"/>s to attach an anchor to an object.
    /// </summary>
    [XmlAttribute("parent")]
    public int ParentId { get; set; }

    /// <summary>
    /// Gets or sets the initial x position of this anchor.
    /// </summary>
    [XmlElement("transform_position_x")]
    public string PositionX { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the initial y position of this anchor.
    /// </summary>
    [XmlElement("transform_position_y")]
    public string PositionY { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the initial z position of this anchor.
    /// </summary>
    [XmlElement("transform_position_z")]
    public string PositionZ { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the string representing the rotation format of this anchor. Should be one of <see cref="RotationFormat"/>.
    /// </summary>
    [XmlAttribute("rotation_format")]
    public string RotationFormat { get; set; }

    /// <summary>
    /// Gets or sets the w component of the initial rotation of this anchor.
    /// </summary>
    [XmlElement("transform_rotation_w")]
    public string RotationW { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the x component of the initial rotation of this anchor.
    /// </summary>
    [XmlElement("transform_rotation_x")]
    public string RotationX { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the y component of the initial rotation of this anchor.
    /// </summary>
    [XmlElement("transform_rotation_y")]
    public string RotationY { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the z component of the initial rotation of this anchor.
    /// </summary>
    [XmlElement("transform_rotation_z")]
    public string RotationZ { get; set; } = "{0.0}";

    /// <summary>
    /// Gets or sets the x component of the initial scale of this anchor.
    /// </summary>
    [XmlElement("transform_scale_x")]
    public string ScaleX { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the y component of the initial scale of this anchor.
    /// </summary>
    [XmlElement("transform_scale_y")]
    public string ScaleY { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the z component of the initial scale of this anchor.
    /// </summary>
    [XmlElement("transform_scale_z")]
    public string ScaleZ { get; set; } = "{1.0}";

    /// <summary>
    /// Gets or sets the units used by the values of this anchor. Should be "m", "cm", or "mm".
    /// </summary>
    [XmlAttribute("units")]
    public string Units { get; set; }
}