using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.WorldTravel
{
    public class PartyTravelPresentationGateTests
    {
        [SetUp]
        public void SetUp()
        {
            PartyTravelPresentationGate.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            PartyTravelPresentationGate.ClearAll();
        }

        [UnityTest]
        public IEnumerator WaitForPresentationCompleteAsync_WaitsForMatchingEpoch()
        {
            int epoch = PartyTravelPresentationGate.BeginPresentation();

            UniTask waitTask = PartyTravelPresentationGate.WaitForPresentationCompleteAsync(epoch);
            yield return null;
            Assert.That(waitTask.GetAwaiter().IsCompleted, Is.False);

            PartyTravelPresentationGate.SignalPresentationComplete(epoch);
            yield return waitTask.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator WaitForPresentationCompleteAsync_WaitsUntilEpochIsRegistered()
        {
            const int expectedEpoch = 3;
            PartyTravelPresentationGate.ClearAll();

            for (int i = 0; i < expectedEpoch; i++)
                PartyTravelPresentationGate.BeginPresentation();

            UniTask waitTask = PartyTravelPresentationGate.WaitForPresentationCompleteAsync(expectedEpoch);
            yield return null;
            Assert.That(waitTask.GetAwaiter().IsCompleted, Is.False);

            PartyTravelPresentationGate.SignalPresentationComplete(expectedEpoch);
            yield return waitTask.ToCoroutine();
        }
    }
}

