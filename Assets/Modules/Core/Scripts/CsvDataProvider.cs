// ------------------------------------------------------------------------------------
// <copyright file="CsvDataProvider.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using IMLD.MixedRealityAnalysis.Network;
using IMLD.MixedRealityAnalysis.Utils;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Storage;
#endif

namespace IMLD.MixedRealityAnalysis.Core
{
    /// <summary>
    /// A data provider for MIRIA based on CSV files.
    /// It loads the meta data from an XML file, which contains references to one or more CSV files containing the actual data.
    /// </summary>
    public class CsvDataProvider : AbstractDataProvider
    {
        private string dataPath;

        /// <summary>
        /// Loads the study with the index given by <paramref name="index"/>.
        /// </summary>
        /// <param name="index">the index of the study to load.</param>
        /// <returns>Task object</returns>
        public override async Task LoadStudyAsync(int index)
        {
            CurrentStudy = StudyList[index];
            Debug.Log("StudyName loaded: " + CurrentStudy.StudyName);
            await LoadStudyDataAsync(StudyList[index]);

            GenerateAnchors();
            PrepareMedia();
            IsStudyLoaded = true;
            CurrentStudyIndex = index;
        }

        /// <summary>
        /// Loads a study from a given xml file.
        /// </summary>
        /// <param name="filepath">The filepath of the study description xml.</param>
        /// <returns>Task object</returns>
        public override async Task LoadStudyAsync(string filepath)
        {
            if (!IsInitialized)
            {
                Initialize();
            }

            StudyData parsedXml = LoadStudyDescription(filepath);
            parsedXml.Id = StudyList.Count;
            StudyList.Add(parsedXml);

            await LoadStudyAsync(parsedXml.Id);
        }

        /// <summary>
        /// Parses a <see cref="Quaternion"/> from the sample data given in three rotation components.
        /// </summary>
        /// <param name="rot_x">The x component of the rotation.</param>
        /// <param name="rot_y">The y component of the rotation.</param>
        /// <param name="rot_z">The z component of the rotation.</param>
        /// <param name="rotationFormat">The rotation format.
        /// Should be <see cref="RotationFormat.EULER_DEG"/>, <see cref="RotationFormat.EULER_RAD"/>, or <see cref="RotationFormat.DIRECTION_VECTOR"/></param>
        /// <returns>The <see cref="Quaternion"/>.</returns>
        internal static Quaternion ParseQuaternionFromSample(string rot_x, string rot_y, string rot_z, RotationFormat rotationFormat)
        {
            float x, y, z;
            x = float.Parse(rot_x, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            y = float.Parse(rot_y, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            z = float.Parse(rot_z, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);

            Quaternion output = new Quaternion();
            if (rotationFormat == RotationFormat.EULER_DEG)
            {
                output.eulerAngles = new Vector3(x, y, z);
            }
            else if (rotationFormat == RotationFormat.EULER_RAD)
            {
                output.eulerAngles = new Vector3(x * Mathf.Rad2Deg, y * Mathf.Rad2Deg, z * Mathf.Rad2Deg);
            }
            else if (rotationFormat == RotationFormat.DIRECTION_VECTOR)
            {
                output.SetLookRotation(new Vector3(x, y, z));
            }

            return output;
        }

        /// <summary>
        /// Parses a <see cref="Quaternion"/> from the sample data given by the four rotation components.
        /// </summary>
        /// <param name="rot_w">The w component of the rotation.</param>
        /// <param name="rot_x">The x component of the rotation.</param>
        /// <param name="rot_y">The y component of the rotation.</param>
        /// <param name="rot_z">The z component of the rotation.</param>
        /// <returns>The <see cref="Quaternion"/>.</returns>
        internal static Quaternion ParseQuaternionFromSample(string rot_w, string rot_x, string rot_y, string rot_z)
        {
            float w, x, y, z;
            w = float.Parse(rot_w, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            x = float.Parse(rot_x, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            y = float.Parse(rot_y, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            z = float.Parse(rot_z, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);

            Quaternion output = new Quaternion(x, y, z, w);
            return output;
        }

        /// <summary>
        /// parses a <see cref="Vector3"/> from the three components.
        /// </summary>
        /// <param name="pos_x">The first component of the vector.</param>
        /// <param name="pos_y">The second component of the vector.</param>
        /// <param name="pos_z">The third component of the vector.</param>
        /// <returns>The <see cref="Vector3"/>.</returns>
        internal static Vector3 ParseVector3FromSample(string pos_x, string pos_y, string pos_z)
        {
            float x, y, z;
            x = float.Parse(pos_x, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            y = float.Parse(pos_y, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            z = float.Parse(pos_z, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);

            Vector3 output = new Vector3(x, y, z);
            return output;
        }

        private static string[] SplitCsvLine(string line)
        {
            return line.Split(',');
        }

        protected override void Start()
        {
            base.Start();
            Initialize();
        }

        /// <summary>
        /// Reads all provided XML files, parses them and stores them in memory,
        /// for easy access. Method can only be called once per run.
        /// </summary>
        private void Initialize()
        {
            // prevents double initialization
            if (IsInitialized)
            {
                return;
            }

            SetDataPath(); // sets the data path
            string[] fileNames = System.IO.Directory.GetFiles(dataPath, "*.xml");

            int counter = 0;
            foreach (string fileName in fileNames)
            {
                StudyData parsedXml = LoadStudyDescription(fileName);
                parsedXml.Id = counter;
                counter++;
                StudyList.Add(parsedXml);
            }

            IsInitialized = true;
            StudyListReady.Invoke();
        }

        private StudyData LoadStudyDescription(string fileName)
        {
            string xmlText = File.ReadAllText(fileName);

            var serializer = new XmlSerializer(typeof(StudyData));
            StudyData parsedXml;
            using (var reader = new StringReader(xmlText))
            {
                parsedXml = (StudyData)serializer.Deserialize(reader);
            }

            return parsedXml;
        }

        /// <summary>
        /// Iterates over all video sources in the current study and hands them over to the ViewContainerManager
        /// </summary>
        private void PrepareMedia()
        {
            if (true /*Services.ContainerManager()*/)
            {
                List<MediaSource> media = new List<MediaSource>();
                foreach (var mediaSource in CurrentStudy.MediaSources)
                {
                    if (mediaSource.File.StartsWith("https"))
                    {
                        media.Add(mediaSource);
                    }
                    else
                    {
                        string filePath = Path.Combine(dataPath, mediaSource.File);
                        if (System.IO.File.Exists(filePath))
                        {
#if UNITY_WSA && !UNITY_EDITOR
                            // As per https://forum.unity.com/threads/url-for-videoplayer-in-uwp.503331/ we copy the file to Application.persistentDataPath, otherwise we get file access violations on UWP/WSA.
                            string wsaFilePath = Path.Combine(Application.persistentDataPath, Math.Abs(filePath.GetHashCode()).ToString()+"_"+Path.GetFileName(filePath));
                            System.IO.File.Copy(filePath, wsaFilePath, true);
                            mediaSource.File = wsaFilePath;
                            media.Add(mediaSource);
#else
                            mediaSource.File = filePath;
                            media.Add(mediaSource);
#endif
                        }
                    }
                }

                CurrentStudy.MediaSources = media;

                //Services.ContainerManager().UpdateVideoSources(media);
            }

        }

        private void GenerateAnchors()
        {
            VisContainers = ParseAnchors(CurrentStudy.Anchors);
            //if (Services.VisManager())
            //{
            //    var visContainers = ParseAnchors(currentStudy.Anchors);
            //    Services.VisManager().DeleteAllViewContainers(false);
            //    foreach (var container in visContainers)
            //    {
            //        Services.VisManager().CreateViewContainer(container, false);
            //    }
            //}            
        }

        private void NormalizeTimestamps()
        {
            // for all condition/session combinations...
            for (int s = 0; s < CurrentStudy.Sessions.Count; s++)
            {
                for (int c = 0; c < CurrentStudy.Conditions.Count; c++)
                {
                    // ... get smallest timestamp of first sample over all tracked objects...
                    long minTimeStamp = long.MaxValue;
                    foreach (var kvp in DataSets)
                    {
                        var dataSet = kvp.Value;
                        if (dataSet.IsStatic)
                        {
                            continue;
                        }

                        minTimeStamp = Math.Min(minTimeStamp, dataSet.GetInfoObjects(s, c)[0].Timestamp);
                    }

                    // ... and normalize all sample timestamps to (value - min)
                    foreach (var kvp in DataSets)
                    {
                        var dataSet = kvp.Value;
                        if (dataSet.IsStatic)
                        {
                            continue;
                        }

                        foreach (var sample in dataSet.GetInfoObjects(s, c))
                        {
                            sample.Timestamp -= minTimeStamp;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads/imports the data for a specific study, defined by the StudyData object
        /// </summary>
        /// <param name="studyXml"> The <see cref="StudyData"/> containing the meta data of the study.</param>
        /// <returns>A <see cref="Task"/> object.</returns>
        private async Task LoadStudyDataAsync(StudyData studyXml)
        {
            // read axis mapping
            ReadAxisMapping(studyXml);

            // 0. read static fields
            DataSets = ReadStaticData(studyXml);

            // 1. generate list of all files from the object sources
            List<string> fileList = GenerateFileList(studyXml);

            // 2. for each file, start a task to read all the data
            List<Task<List<CsvImportBlock>>> tasks = new List<Task<List<CsvImportBlock>>>();
            ////var ImportResults = new List<List<CsvImportBlock>>();
            foreach (var file in fileList)
            {
                tasks.Add(Task.Run(() => ImportFromFile(file, studyXml)));
                ////ImportResults.Add(ImportFromFile(File, studyXml));
            }

            // 3. wait for all tasks to finish
            var importResults = await Task.WhenAll(tasks);

            // 4. combine results
            DataSets = CombineResults(importResults, DataSets);
            foreach (var dataSet in DataSets.Values)
            {
                dataSet.RecomputeBounds();
            }
            ////foreach (var Task in Tasks)
            ////{
            ////    ImportResults.Add(Task.Result);
            ////}
            NormalizeTimestamps();
        }

        private void ReadAxisMapping(StudyData studyXml)
        {
            try
            {
                AxisDirectionX = GetDirectionVectorFromString(studyXml.AxisDirectionX);
                AxisDirectionY = GetDirectionVectorFromString(studyXml.AxisDirectionY);
                AxisDirectionZ = GetDirectionVectorFromString(studyXml.AxisDirectionZ);
            }
            catch (ArgumentException)
            {
                AxisDirectionX = Vector3.right;
                AxisDirectionY = Vector3.up;
                AxisDirectionZ = Vector3.forward;
            }

            AxisTransformationMatrix4x4 = new Matrix4x4(AxisDirectionX, AxisDirectionY, AxisDirectionZ, new Vector4(0, 0, 0, 1));
        }

        private Vector3 GetDirectionVectorFromString(string directionString)
        {
            switch (directionString)
            {
                case "forward":
                    return Vector3.forward;

                case "back":
                    return Vector3.back;

                case "up":
                    return Vector3.up;

                case "down":
                    return Vector3.down;

                case "left":
                    return Vector3.left;

                case "right":
                    return Vector3.right;
            }

            throw new ArgumentException();
        }

        private Dictionary<int, AnalysisObject> ReadStaticData(StudyData studyXml)
        {
            var result = new Dictionary<int, AnalysisObject>();
            foreach (var studyObject in studyXml.Objects)
            {
                int id = studyObject.Id;
                string name = studyObject.Name;
                if (!Enum.TryParse(studyObject.ObjectType, true, out ObjectType Type))
                {
                    Type = ObjectType.UNKNOWN;
                }

                string source = studyObject.Source;
                int parent = studyObject.ParentId;
                Color color = Color.HSVToRGB(studyObject.ColorHue, studyObject.ColorSaturation, studyObject.ColorValue);

                // rotation format
                RotationFormat rotationFormat = RotationFormat.QUATERNION;
                switch (studyObject.RotationFormat)
                {
                    case "euler_deg":
                        rotationFormat = RotationFormat.EULER_DEG;
                        break;

                    case "euler_rad":
                        rotationFormat = RotationFormat.EULER_RAD;
                        break;

                    case "quaternion":
                        rotationFormat = RotationFormat.QUATERNION;
                        break;

                    case "direction_vector":
                        rotationFormat = RotationFormat.DIRECTION_VECTOR;
                        break;
                }

                // time format
                TimeFormat timeFormat = TimeFormat.FLOAT;
                switch (studyObject.TimeFormat)
                {
                    case "float":
                        timeFormat = TimeFormat.FLOAT;
                        break;

                    case "long":
                        timeFormat = TimeFormat.LONG;
                        break;

                    case "string":
                        timeFormat = TimeFormat.STRING;
                        break;
                }

                // scale factor depending on units; we need m
                float unitScaleFactor = 1.0f;
                switch (studyObject.Units)
                {
                    case "mm":
                        unitScaleFactor = 0.001f;
                        break;

                    case "cm":
                        unitScaleFactor = 0.01f;
                        break;

                    case "m":
                        unitScaleFactor = 1f;
                        break;
                }

                var analysisObject = new AnalysisObject(name, id, Type, parent, source, unitScaleFactor, timeFormat, rotationFormat, studyXml.Conditions, studyXml.Sessions, color);

                // parse properties of the object description to get static data
                analysisObject = ParseStaticVariables(studyObject, analysisObject);

                // is the dataset/object static, i.e., not time-dependent?
                analysisObject.IsStatic = studyObject.IsStatic;

                // load model mesh if available
                Mesh objectMesh = null;
                if (studyObject.ModelFile != string.Empty)
                {
                    objectMesh = BasicObjImporter.ImportFromFile(Path.Combine(dataPath, studyObject.ModelFile));
                }

                analysisObject.ObjectModel = objectMesh;

                // add to dictionary
                result.Add(id, analysisObject);
            }

            return result;
        }

        private Dictionary<int, AnalysisObject> CombineResults(List<CsvImportBlock>[] importResults, Dictionary<int, AnalysisObject> dataSets)
        {
            foreach (var list in importResults)
            {
                foreach (var block in list)
                {
                    foreach (var studyObject in dataSets)
                    {
                        if (block.ObjectId == studyObject.Value.Id)
                        {
                            studyObject.Value.SetSamplesForSessionAndCondition(block.Samples, block.SessionId, block.ConditionId, block.MaxSpeed);
                        }
                    }
                }
            }

            return dataSets;
        }

        private List<CsvImportBlock> ImportFromFile(string file, StudyData studyXml)
        {
            // find all object sources for this file
            List<ObjectImportHelper> objectImportHelpers = new List<ObjectImportHelper>();
            foreach (var objectSource in studyXml.ObjectSources)
            {
                if (objectSource.File.Equals(file))
                {
                    objectImportHelpers.Add(new ObjectImportHelper(objectSource, DataSets[objectSource.ObjectId]));
                }
            }

            using (FileStream fs = new FileStream(Path.Combine(dataPath, file), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    // read header (first line in CSV)
                    string header = sr.ReadLine();
                    string[] lineArray = SplitCsvLine(header);

                    // parse the header: iterate over object importers to extract necessary field ids from the header
                    foreach (var objectImporter in objectImportHelpers)
                    {
                        // get object
                        var studyObject = studyXml.Objects[objectImporter.ObjectId];
                        int rot_w = -1, rot_x = -1, rot_y = -1, rot_z = -1, pos_x = -1, pos_y = -1, pos_z = -1, scale_x = -1, scale_y = -1, scale_z = -1, time = -1, state = -1;

                        // for each column, let's find out what is saved in there.
                        for (int i = 0; i < lineArray.Length; i++)
                        {
                            if (lineArray[i].Equals(studyObject.TimestampSource))
                            {
                                time = i;
                            }
                            else if (lineArray[i].Equals(studyObject.PositionXSource))
                            {
                                pos_x = i;
                            }
                            else if (lineArray[i].Equals(studyObject.PositionYSource))
                            {
                                pos_y = i;
                            }
                            else if (lineArray[i].Equals(studyObject.PositionZSource))
                            {
                                pos_z = i;
                            }
                            else if (lineArray[i].Equals(studyObject.RotationWSource))
                            {
                                rot_w = i;
                            }
                            else if (lineArray[i].Equals(studyObject.RotationXSource))
                            {
                                rot_x = i;
                            }
                            else if (lineArray[i].Equals(studyObject.RotationYSource))
                            {
                                rot_y = i;
                            }
                            else if (lineArray[i].Equals(studyObject.RotationZSource))
                            {
                                rot_z = i;
                            }
                            else if (lineArray[i].Equals(studyObject.ScaleXSource))
                            {
                                scale_x = i;
                            }
                            else if (lineArray[i].Equals(studyObject.ScaleYSource))
                            {
                                scale_y = i;
                            }
                            else if (lineArray[i].Equals(studyObject.ScaleZSource))
                            {
                                scale_z = i;
                            }
                            else if (lineArray[i].Equals(studyObject.StateSource))
                            {
                                state = i;
                            }
                            else if (lineArray[i].Equals(objectImporter.Source.ConditionFilterColumn))
                            {
                                objectImporter.ConditionFilterId = i;
                            }
                            else if (lineArray[i].Equals(objectImporter.Source.SessionFilterColumn))
                            {
                                objectImporter.SessionFilterId = i;
                            }

                            // could look for other data here...
                        }

                        if (time != -1)
                        {
                            objectImporter.Parsers.Add(objectImporter.ParseTimestamp);
                            objectImporter.ParserIndices.Add(new int[] { time });
                        }

                        if (state != -1)
                        {
                            DataSets[studyObject.Id].HasStateData = true;
                            objectImporter.Parsers.Add(objectImporter.ParseState);
                            objectImporter.ParserIndices.Add(new int[] { state });
                        }

                        if (pos_x != -1 && pos_y != -1 && pos_z != -1)
                        {
                            objectImporter.Parsers.Add(objectImporter.ParsePosition);
                            objectImporter.ParserIndices.Add(new int[] { pos_x, pos_y, pos_z });
                        }

                        if (scale_x != -1 && scale_y != -1 && scale_z != -1)
                        {
                            objectImporter.Parsers.Add(objectImporter.ParseScale);
                            objectImporter.ParserIndices.Add(new int[] { scale_x, scale_y, scale_z });
                        }

                        if (rot_x != -1 && rot_y != -1 && rot_z != -1)
                        {
                            if (rot_w != -1)
                            {
                                objectImporter.Parsers.Add(objectImporter.ParseRotationWXYZ);
                                objectImporter.ParserIndices.Add(new int[] { rot_w, rot_x, rot_y, rot_z });
                            }
                            else
                            {
                                objectImporter.Parsers.Add(objectImporter.ParseRotationXYZ);
                                objectImporter.ParserIndices.Add(new int[] { rot_x, rot_y, rot_z });
                            }
                        }
                    }

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineArray = SplitCsvLine(line);
                        //foreach (var objectImporter in objectImportHelpers)
                        //{
                        //    objectImporter.ParseSample(lineArray);
                        //}
                        for (int i = 0; i < objectImportHelpers.Count; i++)
                        {
                            objectImportHelpers[i].ParseSample(lineArray);
                        }
                    }
                }
            }

            // fill list with imported data
            List<CsvImportBlock> results = new List<CsvImportBlock>();
            foreach (var objectImporter in objectImportHelpers)
            {
                // pre-compute speed in samples
                if (objectImporter.ImportData.Samples.Count > 1)
                {
                    float maxSpeed = 0f;
                    var samples = objectImporter.ImportData.Samples;
                    for (int i = 1; i < samples.Count; i++)
                    {
                        float deltaPosition = (samples[i].Position - samples[i - 1].Position).magnitude;
                        float deltaTime = (float)(samples[i].Timestamp - samples[i - 1].Timestamp) / TimeSpan.TicksPerSecond;
                        samples[i].Speed = deltaPosition / deltaTime;
                        if (float.IsNaN(samples[i].Speed) || float.IsInfinity(samples[i].Speed))
                        {
                            samples[i].Speed = samples[i - 1].Speed;
                        }

                        if (samples[i].Speed > maxSpeed)
                        {
                            maxSpeed = samples[i].Speed;
                            objectImporter.ImportData.MaxSpeed = maxSpeed;
                        }
                    }
                }

                // add samples
                results.Add(objectImporter.ImportData);
            }
            return results;
        }

        private List<string> GenerateFileList(StudyData studyXml)
        {
            List<string> fileList = new List<string>();
            foreach (var objectSource in studyXml.ObjectSources)
            {
                string fileName = objectSource.File;
                if (fileList.Contains(fileName) == false)
                {
                    fileList.Add(fileName);
                }
            }

            return fileList;
        }

        private List<VisContainer> ParseAnchors(List<VisAnchor> anchorXml)
        {
            var results = new List<VisContainer>();
            foreach (var anchor in anchorXml)
            {
                // create struct with default data
                var visContainer = new VisContainer
                {
                    Position = new float[] { 0, 0, 0 },
                    Scale = new float[] { 1, 1, 1 },
                    Orientation = new float[] { 0, 0, 0, 1 },
                    Id = anchor.Id,
                    ParentId = anchor.ParentId,
                };
                float unitScaleFactor = 1.0f;
                switch (anchor.Units)
                {
                    case "mm":
                        unitScaleFactor = 0.001f;
                        break;

                    case "cm":
                        unitScaleFactor = 0.01f;
                        break;

                    case "m":
                        unitScaleFactor = 1f;
                        break;
                }

                // rotation format
                RotationFormat rotationFormat = RotationFormat.QUATERNION;
                switch (anchor.RotationFormat)
                {
                    case "euler_deg":
                        rotationFormat = RotationFormat.EULER_DEG;
                        break;

                    case "euler_rad":
                        rotationFormat = RotationFormat.EULER_RAD;
                        break;

                    case "quaternion":
                        rotationFormat = RotationFormat.QUATERNION;
                        break;

                    case "direction_vector":
                        rotationFormat = RotationFormat.DIRECTION_VECTOR;
                        break;
                }

                if (IsLiteral(anchor.PositionX) && IsLiteral(anchor.PositionY) && IsLiteral(anchor.PositionZ))
                {
                    var position = ParseStaticPosition(anchor.PositionX, anchor.PositionY, anchor.PositionZ, unitScaleFactor);
                    visContainer.Position = new float[] { position.x, position.y, position.z };
                }

                if (IsLiteral(anchor.ScaleX) && IsLiteral(anchor.ScaleY) && IsLiteral(anchor.ScaleZ))
                {
                    var scale = ParseStaticScale(anchor.ScaleX, anchor.ScaleY, anchor.ScaleZ);
                    visContainer.Scale = new float[] { scale.x, scale.y, scale.z };
                }

                if (anchor.RotationW.Length == 0)
                {
                    if (IsLiteral(anchor.RotationX) && IsLiteral(anchor.RotationY) && IsLiteral(anchor.RotationZ))
                    {
                        var rotation = ParseStaticRotation(anchor.RotationX, anchor.RotationY, anchor.RotationZ, rotationFormat);
                        visContainer.Orientation = new float[] { rotation.x, rotation.y, rotation.z, rotation.w };
                    }
                }
                else
                {
                    if (IsLiteral(anchor.RotationW) && IsLiteral(anchor.RotationX) && IsLiteral(anchor.RotationY) && IsLiteral(anchor.RotationZ))
                    {
                        var rotation = ParseStaticRotation(anchor.RotationW, anchor.RotationX, anchor.RotationY, anchor.RotationZ);
                        visContainer.Orientation = new float[] { rotation.x, rotation.y, rotation.z, rotation.w };
                    }
                }

                results.Add(visContainer);
            }

            return results;
        }

        private AnalysisObject ParseStaticVariables(StudyObject objectXml, AnalysisObject analysisObject)
        {
            // variables used during parsing of the data
            Vector3 staticPosition = Vector3.zero;
            Vector3 staticScale = Vector3.one;
            Quaternion staticRotation = Quaternion.identity;
            bool useStaticPosition = false;
            bool useStaticScale = false;
            bool useStaticRotation = false;
            float unitScaleFactor = analysisObject.UnitConversionFactor;

            if (objectXml.PositionXSource.Length == 0 || objectXml.PositionYSource.Length == 0 || objectXml.PositionZSource.Length == 0)
            {
                staticPosition = Vector3.zero;
                useStaticPosition = true;
            }
            else if (IsLiteral(objectXml.PositionXSource) && IsLiteral(objectXml.PositionYSource) && IsLiteral(objectXml.PositionZSource))
            {
                staticPosition = ParseStaticPosition(objectXml.PositionXSource, objectXml.PositionYSource, objectXml.PositionZSource, unitScaleFactor);
                useStaticPosition = true;
            }

            if (objectXml.ScaleXSource.Length == 0 || objectXml.ScaleYSource.Length == 0 || objectXml.ScaleZSource.Length == 0)
            {
                staticScale = Vector3.one;
                useStaticScale = true;
            }
            else if (IsLiteral(objectXml.ScaleXSource) && IsLiteral(objectXml.ScaleYSource) && IsLiteral(objectXml.ScaleZSource))
            {
                staticScale = ParseStaticScale(objectXml.ScaleXSource, objectXml.ScaleYSource, objectXml.ScaleZSource);
                useStaticScale = true;
            }

            if (objectXml.RotationXSource.Length == 0 || objectXml.RotationYSource.Length == 0 || objectXml.RotationZSource.Length == 0)
            {
                staticRotation = Quaternion.identity;
                useStaticRotation = true;
            }
            else if (objectXml.RotationWSource.Length == 0)
            {
                if (IsLiteral(objectXml.RotationXSource) && IsLiteral(objectXml.RotationYSource) && IsLiteral(objectXml.RotationZSource))
                {
                    staticRotation = ParseStaticRotation(objectXml.RotationXSource, objectXml.RotationYSource, objectXml.RotationZSource, objectXml.RotationFormat);
                    useStaticRotation = true;
                }
            }
            else
            {
                if (IsLiteral(objectXml.RotationWSource) && IsLiteral(objectXml.RotationXSource) && IsLiteral(objectXml.RotationYSource) && IsLiteral(objectXml.RotationZSource))
                {
                    staticRotation = ParseStaticRotation(objectXml.RotationWSource, objectXml.RotationXSource, objectXml.RotationYSource, objectXml.RotationZSource);
                    useStaticRotation = true;
                }
            }

            // assign static data to analysis object
            analysisObject.UseStaticPosition = useStaticPosition;
            analysisObject.UseStaticRotation = useStaticRotation;
            analysisObject.UseStaticScale = useStaticScale;
            analysisObject.LocalPosition = staticPosition;
            analysisObject.LocalRotation = staticRotation;
            analysisObject.LocalScale = staticScale;

            return analysisObject;
        }

        private Vector3 ParseStaticPosition(string posX, string posY, string posZ, float unitScaleFactor)
        {
            Vector3 result = unitScaleFactor * ParseVector3FromSample(posX.Substring(1, posX.Length - 2), posY.Substring(1, posY.Length - 2), posZ.Substring(1, posZ.Length - 2));

            Vector4 vector = new Vector4(result.x, result.y, result.z, 0);
            vector = AxisTransformationMatrix4x4 * vector;
            result.x = vector.x;
            result.y = vector.y;
            result.z = vector.z;

            return result;
        }

        private Vector3 ParseStaticScale(string scaleX, string scaleY, string scaleZ)
        {
            return ParseVector3FromSample(scaleX.Substring(1, scaleX.Length - 2), scaleY.Substring(1, scaleY.Length - 2), scaleZ.Substring(1, scaleZ.Length - 2));
        }

        private Quaternion ParseStaticRotation(string rotX, string rotY, string rotZ, RotationFormat rotationFormat)
        {
            Quaternion result = ParseQuaternionFromSample(rotX.Substring(1, rotX.Length - 2), rotY.Substring(1, rotY.Length - 2), rotZ.Substring(1, rotZ.Length - 2), rotationFormat);

            result.Normalize();
            Matrix4x4 rotMatrixData = Matrix4x4.Rotate(result);
            Matrix4x4 rotMatrixUnity = AxisTransformationMatrix4x4 * rotMatrixData * AxisTransformationMatrix4x4.inverse;
            result = rotMatrixUnity.rotation;

            return result;
        }

        private Quaternion ParseStaticRotation(string rotW, string rotX, string rotY, string rotZ)
        {
            Quaternion result = ParseQuaternionFromSample(rotW.Substring(1, rotW.Length - 2), rotX.Substring(1, rotX.Length - 2), rotY.Substring(1, rotY.Length - 2), rotZ.Substring(1, rotZ.Length - 2));

            result.Normalize();
            Matrix4x4 rotMatrixData = Matrix4x4.Rotate(result);
            Matrix4x4 rotMatrixUnity = AxisTransformationMatrix4x4 * rotMatrixData * AxisTransformationMatrix4x4.inverse;
            result = rotMatrixUnity.rotation;

            return result;
        }

        private bool IsLiteral(string input)
        {
            if (input == null || input.Length < 3)
            {
                return false;
            }

            if (input[0] == '{' && input[input.Length - 1] == '}')
            {
                return true;
            }

            return false;
        }

        private void SetDataPath()
        {
#if UNITY_WSA && !UNITY_EDITOR
            dataPath = Windows.Storage.KnownFolders.Objects3D.Path.ToString() + @"\miria_data\";
#else
            dataPath = Application.persistentDataPath + @"\miria_data\";
#endif
        }

        protected override void OnClientConnected(object sender, NetworkManager.NewClientEventArgs e)
        {
            // send client information about study
            if(Services.DataManager() != null && Services.NetworkManager() != null)
            {
                var studyMessage = new MessageLoadStudy(Services.DataManager().CurrentStudyIndex);
                Services.NetworkManager().SendMessage(studyMessage.Pack(), e.ClientToken);
            }            
        }

        /// <summary>
        /// Helper class used to store one block of <see cref="Sample"/>s.
        /// </summary>
        private class CsvImportBlock
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CsvImportBlock"/> class.
            /// The <paramref name="source"/> provides the <see cref="ObjectId"/>, <see cref="ConditionId"/>, and <see cref="SessionId"/>.
            /// </summary>
            /// <param name="source">The <see cref="ObjectSource"/>.</param>
            public CsvImportBlock(ObjectSource source)
            {
                ObjectId = source.ObjectId;
                SessionId = source.SessionId;
                ConditionId = source.ConditionId;
                Samples = new List<Sample>();
            }

            /// <summary>
            /// Gets or sets the id of the <see cref="AnalysisObject"/> this data belongs to.
            /// </summary>
            public int ObjectId { get; set; }

            /// <summary>
            /// Gets or sets the id of the session this data belongs to.
            /// </summary>
            public int SessionId { get; set; }

            /// <summary>
            /// Gets or sets the id of the condition this data belongs to.
            /// </summary>
            public int ConditionId { get; set; }

            /// <summary>
            /// Gets or sets the list of samples.
            /// </summary>
            public List<Sample> Samples { get; set; }

            /// <summary>
            /// Gets or sets the maximum speed in this block of samples.
            /// </summary>
            public float MaxSpeed { get; set; } = 0f;
        }

        /// <summary>
        /// Helper class that provides functionality to parse data samples and add them to a <see cref="CsvImportBlock"/>.
        /// </summary>
        private class ObjectImportHelper
        {
            private Sample currentSample;
            private TimeFormat timeFormat;
            private Matrix4x4 axisTransformationMatrix4x4;

            /// <summary>
            /// Initializes a new instance of the <see cref="ObjectImportHelper"/> class.
            /// </summary>
            /// <param name="source">The <see cref="ObjectSource"/> containing the meta data of the object.</param>
            /// <param name="analysisObject">The <see cref="AnalysisObject"/> already containing the static data of the object.</param>
            public ObjectImportHelper(ObjectSource source, AnalysisObject analysisObject)
            {
                Source = source;
                ObjectId = source.ObjectId;
                SessionId = source.SessionId;
                ConditionId = source.ConditionId;
                ImportData = new CsvImportBlock(source);
                Parsers = new List<ParserFunc>();
                ParserIndices = new List<int[]>();
                StaticPosition = analysisObject.LocalPosition;
                StaticOrientation = analysisObject.LocalRotation;
                StaticScale = analysisObject.LocalScale;
                UnitConversionFactor = analysisObject.UnitConversionFactor;
                RotationFormat = analysisObject.RotationFormat;
                timeFormat = analysisObject.TimeFormat;
                if (Services.DataManager() != null)
                {
                    axisTransformationMatrix4x4 = Services.DataManager().AxisTransformationMatrix4x4;
                }
                else
                {
                    axisTransformationMatrix4x4 = Matrix4x4.identity;
                }
            }

            /// <summary>
            /// A parser function that adds data to the current sample by parsing from the input data.
            /// </summary>
            /// <param name="inputData">The string array to parse the data from.</param>
            /// <param name="indices">The array of indices indicating which cells of the input data to parse from.</param>
            public delegate void ParserFunc(string[] inputData, int[] indices);

            public ObjectSource Source { get; set; }

            public int ObjectId { get; set; }

            public int SessionId { get; set; }

            public int ConditionId { get; set; }

            public int ConditionFilterId { get; set; } = -1;

            public int SessionFilterId { get; set; } = -1;

            public CsvImportBlock ImportData { get; set; }

            public List<ParserFunc> Parsers { get; set; }

            public List<int[]> ParserIndices { get; set; }

            public Vector3 StaticPosition { get; set; }

            public Quaternion StaticOrientation { get; set; }

            public Vector3 StaticScale { get; set; }

            public RotationFormat RotationFormat { get; set; } = RotationFormat.QUATERNION;

            public float UnitConversionFactor { get; set; }

            /// <summary>
            /// Parses the next sample from the given input data.
            /// </summary>
            /// <param name="input">The input data.</param>
            public void ParseSample(string[] input)
            {
                // check filter status and return if we need to skip this sample
                if (ConditionFilterId != -1)
                {
                    if (Source.ConditionFilter.Equals(input[ConditionFilterId]) != true)
                    {
                        return;
                    }
                }

                if (SessionFilterId != -1)
                {
                    if (Source.SessionFilter.Equals(input[SessionFilterId]) != true)
                    {
                        return;
                    }
                }

                // set static data
                currentSample = new Sample(StaticPosition, StaticOrientation, StaticScale);

                // iterate over all fields of the CSV file that are used to fill this sample and call the stored parsers
                for (int i = 0; i < Parsers.Count; i++)
                {
                    Parsers[i](input, ParserIndices[i]);
                }

                ImportData.Samples.Add(currentSample);
            }

            /// <summary>
            /// Parses a position from the input data
            /// </summary>
            /// <param name="input">The input data.</param>
            /// <param name="indices">The indices in the data to parse from.</param>
            public void ParsePosition(string[] input, int[] indices)
            {
                Vector3 result = UnitConversionFactor * CsvDataProvider.ParseVector3FromSample(input[indices[0]], input[indices[1]], input[indices[2]]);

                Vector4 vector = new Vector4(result.x, result.y, result.z, 0);
                vector = axisTransformationMatrix4x4 * vector;
                result.x = vector.x;
                result.y = vector.y;
                result.z = vector.z;

                currentSample.Position = result;
            }

            /// <summary>
            /// Parses a scale from the input data
            /// </summary>
            /// <param name="input">The input data.</param>
            /// <param name="indices">The indices in the data to parse from.</param>
            public void ParseScale(string[] input, int[] indices)
            {
                currentSample.Scale = CsvDataProvider.ParseVector3FromSample(input[indices[0]], input[indices[1]], input[indices[2]]);
            }

            /// <summary>
            /// Parses a state from the input data
            /// </summary>
            /// <param name="input">The input data.</param>
            /// <param name="indices">The indices in the data to parse from.</param>
            public void ParseState(string[] input, int[] indices)
            {
                currentSample.State = input[indices[0]];
            }

            /// <summary>
            /// Parses a rotation from four components of the input data
            /// </summary>
            /// <param name="input">The input data.</param>
            /// <param name="indices">The indices in the data to parse from.</param>
            public void ParseRotationWXYZ(string[] input, int[] indices)
            {
                Quaternion result = CsvDataProvider.ParseQuaternionFromSample(input[indices[0]], input[indices[1]], input[indices[2]], input[indices[3]]);
                result.Normalize();
                Matrix4x4 rotMatrixData = Matrix4x4.Rotate(result);
                Matrix4x4 rotMatrixUnity = axisTransformationMatrix4x4 * rotMatrixData * axisTransformationMatrix4x4.inverse;
                result = rotMatrixUnity.rotation;

                currentSample.Rotation = result;
            }

            /// <summary>
            /// Parses a position from three components of the input data
            /// </summary>
            /// <param name="input">The input data.</param>
            /// <param name="indices">The indices in the data to parse from.</param>
            public void ParseRotationXYZ(string[] input, int[] indices)
            {
                // matrix variant
                Quaternion result = CsvDataProvider.ParseQuaternionFromSample(input[indices[0]], input[indices[1]], input[indices[2]], RotationFormat);
                result.Normalize();
                Matrix4x4 rotMatrixData = Matrix4x4.Rotate(result);
                Matrix4x4 rotMatrixUnity = axisTransformationMatrix4x4 * rotMatrixData * axisTransformationMatrix4x4.inverse;
                result = rotMatrixUnity.rotation;

                currentSample.Rotation = result;
            }

            /// <summary>
            /// Parses a timestamp from the input data
            /// </summary>
            /// <param name="input">The input data.</param>
            /// <param name="indices">The indices in the data to parse from.</param>
            public void ParseTimestamp(string[] input, int[] indices)
            {
                long output;
                long parsedLongValue;
                float parsedFloatValue;
                if (timeFormat == TimeFormat.LONG)
                {
                    if (long.TryParse(input[indices[0]], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out parsedLongValue))
                    {
                        output = parsedLongValue;
                    }
                    else if (float.TryParse(input[indices[0]], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out parsedFloatValue))
                    {
                        output = (long)((double)parsedFloatValue * TimeSpan.TicksPerSecond);
                        timeFormat = TimeFormat.FLOAT;
                    }
                    else if (TryParseTime(input[indices[0]], out parsedLongValue))
                    {
                        output = parsedLongValue;
                        timeFormat = TimeFormat.STRING;
                    }
                    else
                    {
                        output = 0;
                        Debug.LogError("No valid timestamps found in CSV!");
                    }
                }
                else if (timeFormat == TimeFormat.FLOAT)
                {
                    if (float.TryParse(input[indices[0]], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out parsedFloatValue))
                    {
                        output = (long)((double)parsedFloatValue * TimeSpan.TicksPerSecond);
                    }
                    else if (long.TryParse(input[indices[0]], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out parsedLongValue))
                    {
                        output = parsedLongValue;
                        timeFormat = TimeFormat.LONG;
                    }
                    else if (TryParseTime(input[indices[0]], out parsedLongValue))
                    {
                        output = parsedLongValue;
                        timeFormat = TimeFormat.STRING;
                    }
                    else
                    {
                        output = 0;
                        Debug.LogError("No valid timestamps found in CSV!");
                    }
                }
                else
                {
                    if (TryParseTime(input[indices[0]], out parsedLongValue))
                    {
                        output = parsedLongValue;
                    }
                    else if (long.TryParse(input[indices[0]], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out parsedLongValue))
                    {
                        output = parsedLongValue;
                        timeFormat = TimeFormat.LONG;
                    }
                    else if (float.TryParse(input[indices[0]], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out parsedFloatValue))
                    {
                        output = (long)((double)parsedFloatValue * TimeSpan.TicksPerSecond);
                        timeFormat = TimeFormat.FLOAT;
                    }
                    else
                    {
                        output = 0;
                        Debug.LogError("No valid timestamps found in CSV!");
                    }
                }

                currentSample.Timestamp = output;
            }

            private bool TryParseTime(string timestamp, out long parsedValue)
            {
                if (timestamp.Contains(' '))
                {
                    string[] strArr = timestamp.Split(' ');
                    timestamp = strArr[strArr.Length - 1];
                }

                parsedValue = 0;
                string[] subs = timestamp.Split(new char[] { ':', '.' });
                if (subs.Length != 4)
                {
                    return false;
                }

                if (!int.TryParse(subs[0], out int Hours))
                {
                    return false;
                }

                if (!int.TryParse(subs[1], out int Minutes))
                {
                    return false;
                }

                if (!int.TryParse(subs[2], out int Seconds))
                {
                    return false;
                }

                if (!int.TryParse(subs[3], out int Millis))
                {
                    return false;
                }

                parsedValue = (Hours * TimeSpan.TicksPerHour) + (Minutes * TimeSpan.TicksPerMinute) + (Seconds * TimeSpan.TicksPerSecond) + (Millis * TimeSpan.TicksPerMillisecond);
                return true;
            }
        }
    }
}