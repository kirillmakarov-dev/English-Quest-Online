using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime.Fixtures
{
    public abstract class CombatTestMultiPeerFixture
    {
        private static readonly HashSet<Type> BootstrappedTypes = new();

        protected readonly List<NetworkRunner> Runners = new();

        protected IEnumerator BootstrapCombatSceneIfNeededIgnoringLogs() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(BootstrapCombatSceneIfNeeded());

        protected IEnumerator BootstrapCombatSceneIfNeeded()
        {
            Type testType = GetType();
            if (!BootstrappedTypes.Contains(testType))
            {
                yield return FullBootstrap(testType);
                yield break;
            }

            yield return RefreshRunners();
        }

        protected IEnumerator RefreshRunners()
        {
            Runners.Clear();
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    Runners.Add(runner);
            }

            if (Runners.Count < 2)
            {
                BootstrappedTypes.Remove(GetType());
                yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();
                yield return null;
                yield return FullBootstrap(GetType());
                yield break;
            }

            yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, Runners);
            FusionMultiPeerTestSupport.EnsureSocialServicesForRunners(Runners);
        }

        private IEnumerator FullBootstrap(Type testType)
        {
            FusionMultiPeerTestSupport.AssertMultiPeerMode();
            yield return FusionMultiPeerTestSupport.LoadCombatTestScene();
            yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, Runners);
            BootstrappedTypes.Add(testType);
        }
    }
}

