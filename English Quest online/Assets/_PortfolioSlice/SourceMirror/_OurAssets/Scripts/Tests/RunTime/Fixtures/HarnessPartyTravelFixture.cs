using System;
using System.Collections;
using System.Collections.Generic;
using EnglishQuest.Tests;
using Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime.Fixtures
{
    [Category(TestCategories.Fast)]
    public abstract class HarnessPartyTravelFixture
    {
        private static readonly HashSet<Type> BootstrappedTypes = new();

        protected readonly List<NetworkRunner> Runners = new();

        [UnitySetUp]
        public IEnumerator EnsureClassBootstrap() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(ClassBootstrapBody());

        private IEnumerator ClassBootstrapBody()
        {
            WorldTravelResilienceTestSupport.ResetTravelTransientState();
            FusionHarnessFactory.InitializeHarnessMapRegistry();
            FusionHarnessFactory.EnsureHarnessContentIfNeeded();

            Type testType = GetType();
            if (!BootstrappedTypes.Contains(testType))
            {
                FusionMultiPeerTestSupport.AssertMultiPeerMode();
                yield return WorldTravelTestSupport.BootstrapHarnessMultiPeer();
                yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, Runners);
                yield return WorldTravelTestSupport.WaitForWorldTravelReady(
                    FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                    "World travel systems should be ready in the harness.");
                yield return WorldTravelResilienceTestSupport.PrepareParty(Runners[0], Runners[1]);
                BootstrappedTypes.Add(testType);
            }
            else
            {
                yield return RefreshRunners();

                if (!AreRunnersInHarnessOrigin(Runners))
                {
                    TestDebugLog.Warn("Harness", "Runners left harness origin; re-bootstrapping.");
                    BootstrappedTypes.Remove(testType);
                    yield return WorldTravelTestSupport.BootstrapHarnessMultiPeer();
                    yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, Runners);
                    yield return WorldTravelTestSupport.WaitForWorldTravelReady(
                        FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                        "World travel systems should be ready after origin reset.");
                    yield return WorldTravelResilienceTestSupport.PrepareParty(Runners[0], Runners[1]);
                    BootstrappedTypes.Add(testType);
                }
                else if (!FusionMultiPeerTestSupport.ArePlayersInSameParty(
                             Runners[0],
                             Runners[1],
                             Runners[0].LocalPlayer))
                {
                    yield return WorldTravelResilienceTestSupport.PrepareParty(Runners[0], Runners[1]);
                }
            }

            WorldMapUI staleMapUi = WorldTravelTestSupport.FindWorldMapUi();
            if (staleMapUi != null && staleMapUi.IsOpen)
                staleMapUi.Close();
        }

        [TearDown]
        public void PerTestCleanup()
        {
            WorldTravelResilienceTestSupport.ResetTravelTransientState();
        }

        private IEnumerator RefreshRunners()
        {
            Runners.Clear();
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    Runners.Add(runner);
            }

            if (Runners.Count < 2)
            {
                TestDebugLog.Warn("Harness", "Runners degraded; re-bootstrapping harness party fixture.");
                BootstrappedTypes.Remove(GetType());
                yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();
                yield return null;
                yield return WorldTravelTestSupport.BootstrapHarnessMultiPeer();
                yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, Runners);
                yield return WorldTravelTestSupport.WaitForWorldTravelReady(
                    FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                    "World travel systems should be ready after harness re-bootstrap.");
                yield return WorldTravelResilienceTestSupport.PrepareParty(Runners[0], Runners[1]);
                BootstrappedTypes.Add(GetType());
                yield break;
            }

            yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, Runners);
            FusionMultiPeerTestSupport.EnsureSocialServicesForRunners(Runners);
        }

        private static bool AreRunnersInHarnessOrigin(IReadOnlyList<NetworkRunner> runners)
        {
            if (runners == null || runners.Count == 0)
                return false;

            for (int i = 0; i < runners.Count; i++)
            {
                if (!WorldTravelTestSupport.IsRunnerInDestination(
                        runners[i],
                        FusionHarnessFactory.HarnessOriginSceneName,
                        FusionHarnessFactory.HarnessOriginSpawnId))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

