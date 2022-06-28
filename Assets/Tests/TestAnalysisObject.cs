using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    [TestFixture]
    public class TestAnalysisObject
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestAnalysisObjectFunctions()
        {
            // setup data for test object
            string title = "test_title";
            int id = 0;
            ObjectType type = ObjectType.TRACKABLE;
            int parentId = -1;
            string dataSource = "test_data_source";
            float unitfactor = 1.0f;
            TimeFormat timeformat = TimeFormat.FLOAT;
            RotationFormat rotationformat = RotationFormat.QUATERNION;
            List<string> conditions = new List<string>();
            List<Session> sessions = new List<Session>();
            Color color = Color.red;

            conditions.Add("test_condition");

            Session testSession = new Session()
            {
                Id = 0, Name = "test_session"
            };

            sessions.Add(testSession);

            // test constructor
            AnalysisObject testObject = new AnalysisObject(title, id, type, parentId, dataSource, unitfactor, timeformat, rotationformat, conditions, sessions, color);

            Assert.IsTrue(testObject.Title == title &&
                testObject.Id == id &&
                testObject.ObjectType == type &&
                testObject.ParentObjectId == parentId &&
                testObject.ObjectDataSource == dataSource &&
                testObject.UnitConversionFactor == unitfactor &&
                testObject.TimeFormat == timeformat &&
                testObject.RotationFormat == rotationformat &&
                testObject.ConditionCount == conditions.Count &&
                testObject.SessionCount == sessions.Count &&
                testObject.ObjectColor == color, "Failed to construct AnalysisObject.");
            Debug.Log("TEST OK: Constructed AnalysisObject.");

            // test setting samples         
            long timestamp = DateTime.Now.Ticks;
            List<Sample> samples = new List<Sample>();
            for (int i = 0; i < 10; i++)
            {
                Vector3 position = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                Quaternion orientation = Quaternion.Euler(UnityEngine.Random.Range(0.0f, 180.0f), UnityEngine.Random.Range(0.0f, 180.0f), UnityEngine.Random.Range(0.0f, 180.0f));
                Vector3 scale = Vector3.one;
                Sample sample = new Sample(position, orientation, scale);
                sample.Timestamp = timestamp + (long)(TimeSpan.TicksPerSecond * i * 0.1f);
                samples.Add(sample);
            }

            const float maxspeed = 2.5f;
            testObject.SetSamplesForSessionAndCondition(samples, 0, 0, maxspeed);
            Assert.IsTrue(testObject.GetInfoObjects(0, 0).Count == samples.Count && testObject.GetMaxSpeed(0,0) == maxspeed, "Failed to set samples.");
            Debug.Log("TEST OK: Set samples.");

            Assert.IsTrue(testObject.GetIndexFromTimestamp(timestamp + (long)(TimeSpan.TicksPerSecond * 4 * 0.1f), 0, 0) == 4, "Failed to compute index from timestamp");
            Debug.Log("TEST OK: Computed index from timestamp.");

            Assert.IsTrue(testObject.GetMaxTimestamp(0, 0) == timestamp + (long)(TimeSpan.TicksPerSecond * 9 * 0.1f), "Failed to get maximum timestamp.");
            Debug.Log("TEST OK: Returned maximum timestamp.");

            Assert.IsTrue(testObject.GetMinTimestamp(0, 0) == timestamp, "Failed to get minimum timestamp.");
            Debug.Log("TEST OK: Returned minimum timestamp.");
        }
    }
}