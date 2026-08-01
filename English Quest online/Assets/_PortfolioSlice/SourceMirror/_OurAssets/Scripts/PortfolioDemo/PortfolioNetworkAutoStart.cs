using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    /// <summary>
    /// Starts the portfolio scene as a Shared Fusion session in every player instance.
    /// All instances use the same profile, so the first creates the room and the second joins it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PortfolioNetworkAutoStart : MonoBehaviour
    {
        [SerializeField] private NetworkSessionProfile sessionProfile;
        [SerializeField] private bool autoStart = true;

        public void CollectConfigurationIssues(List<string> errors, List<string> warnings)
        {
            errors ??= new List<string>();
            warnings ??= new List<string>();

            if (!autoStart)
                warnings.Add("PortfolioNetworkAutoStart has autoStart disabled.");

            if (sessionProfile == null)
                errors.Add("PortfolioNetworkAutoStart is missing its NetworkSessionProfile binding.");

            if (GetComponent<GameNetworkManager>() == null)
                errors.Add("PortfolioNetworkAutoStart is not hosted on the GameNetworkManager object.");
        }

        private void Start()
        {
            StartSessionAsync().Forget();
        }

        private async UniTaskVoid StartSessionAsync()
        {
            if (!autoStart)
                return;

            await UniTask.Yield();

            GameNetworkManager manager = GetComponent<GameNetworkManager>();
            if (manager == null || manager != GameNetworkManager.Instance)
                return;

            NetworkRunner runner = manager.Runner;
            if (runner != null && runner.IsRunning)
                return;

            if (sessionProfile == null)
            {
                AppLog.Error("[PortfolioNetworkAutoStart] Network session profile is not assigned.");
                return;
            }

            AppLog.Info(
                $"[PortfolioNetworkAutoStart] Joining Shared room '{sessionProfile.SessionName}'.");

            await manager.StartSharedSession(sessionProfile);
        }
    }
}
