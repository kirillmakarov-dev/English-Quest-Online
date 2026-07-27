using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.WorldTravel
{
    public class SceneLoadWaitUtilityTests
    {
        private LogLevel _previousLogLevel;

        [SetUp]
        public void SetUp()
        {
            _previousLogLevel = AppLog.Level;
            AppLog.Level = LogLevel.Error;
        }

        [TearDown]
        public void TearDown()
        {
            AppLog.Level = _previousLogLevel;
        }

        [UnityTest]
        public IEnumerator WaitForSceneActiveAsync_ReturnsTrueWhenSceneAlreadyActive()
        {
            Scene scene = SceneManager.GetActiveScene();
            UniTask<bool> waitTask = SceneLoadWaitUtility.WaitForSceneActiveAsync(scene.buildIndex, timeoutSeconds: 1f);
            bool result = default;
            yield return waitTask.ToCoroutine(r => result = r);

            Assert.That(result, Is.True);
        }

        [UnityTest]
        public IEnumerator WaitForSceneActiveAsync_TimesOutForMissingScene()
        {
            const int missingBuildIndex = 99998;
            LogAssert.Expect(
                LogType.Error,
                new Regex(@"\[SceneLoadWaitUtility\] Timed out after .* waiting for scene build index 99998"));
            UniTask<bool> waitTask = SceneLoadWaitUtility.WaitForSceneActiveAsync(
                missingBuildIndex,
                timeoutSeconds: 0.25f);
            bool result = default;
            yield return waitTask.ToCoroutine(r => result = r);

            Assert.That(result, Is.False);
        }
    }
}

