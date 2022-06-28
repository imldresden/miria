using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IMLD.MixedRealityAnalysis.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    [TestFixture]
    public class TestDataProvider
    {
        GameObject dataProviderGameObject;

        [SetUp]
        public void Setup()
        {
            dataProviderGameObject = new GameObject();
            
            dataProviderGameObject.AddComponent<Services>();   

            Services.Instance.DataManagerReference = dataProviderGameObject.AddComponent<CsvDataProvider>();
            Services.Instance.VisManagerReference = dataProviderGameObject.AddComponent<VisualizationManager>();
        }

        //[Test]
        //public async void TestDataProviderFunctionsSimple()
        //{
        //    CsvDataProvider testObject = dataProviderGameObject.GetComponent<CsvDataProvider>();
        //    Assert.IsTrue(testObject.IsInitialized, "Failed to initialize DataProvider.");
        //    Debug.Log("Test OK: DataProvider initizalized.");

        //    await testObject.LoadStudyAsync(Application.streamingAssetsPath + "/test_study.xml");

        //    Assert.IsTrue(testObject.CurrentStudy.StudyName == "Test Study" &&
        //        testObject.CurrentStudy.Conditions.Count == 2 &&
        //        testObject.CurrentStudy.Sessions.Count == 1 &&
        //        testObject.CurrentStudy.Objects.Count == 2, "Failed to load study data.");
        //    Debug.Log("Test OK: Successfully loaded study data.");
        //}

        [UnityTest]
        public IEnumerator TestDataProviderFunctions()
        {
            CsvDataProvider testObject = dataProviderGameObject.GetComponent<CsvDataProvider>();
            Assert.IsTrue(testObject.IsInitialized, "Failed to initialize DataProvider.");
            Debug.Log("Test OK: DataProvider initizalized.");

            //var task = Task.Run(async () => {
            //    await testObject.LoadStudyAsync(Application.streamingAssetsPath + "/test_study.xml");
            //});

            var task = testObject.LoadStudyAsync(Application.streamingAssetsPath + "/test_study.xml");
            while (!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) { throw task.Exception; }

            Assert.IsTrue(testObject.CurrentStudy.StudyName == "Test Study" &&
                    testObject.CurrentStudy.Conditions.Count == 2 &&
                    testObject.CurrentStudy.Sessions.Count == 1 &&
                    testObject.CurrentStudy.Objects.Count == 2, "Failed to load study data.");
            Debug.Log("Test OK: Successfully loaded study data.");
        }
    }
}
