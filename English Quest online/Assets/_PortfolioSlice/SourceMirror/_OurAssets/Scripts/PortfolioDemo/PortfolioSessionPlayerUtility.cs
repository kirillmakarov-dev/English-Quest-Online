using Fusion;

namespace EnglishQuest.PortfolioDemo
{
    public static class PortfolioSessionPlayerUtility
    {
        public static NetworkObject ResolvePlayerObject(NetworkRunner contextRunner, PlayerRef player)
        {
            if (contextRunner == null || !contextRunner.IsRunning || !player.IsRealPlayer)
                return null;

            NetworkObject authoritativeObject = GetPlayerObjectFromAuthoritativeRunner(contextRunner, player);
            if (authoritativeObject != null)
                return authoritativeObject;

            foreach (NetworkRunner sessionRunner in FusionCoSessionRunners.Enumerate(contextRunner))
            {
                if (sessionRunner == null || !sessionRunner.IsRunning)
                    continue;

                NetworkObject playerObject = sessionRunner.GetPlayerObject(player);
                if (playerObject != null)
                    return playerObject;
            }

            return null;
        }

        public static string ResolveDisplayName(NetworkRunner contextRunner, PlayerRef player)
        {
            NetworkObject playerObject = ResolvePlayerObject(contextRunner, player);
            if (playerObject != null && playerObject.TryGetComponent(out PlayerNameSync nameSync))
            {
                string syncedName = nameSync.PlayerName.ToString();
                if (!string.IsNullOrWhiteSpace(syncedName))
                    return syncedName;
            }

            return $"Player {player.PlayerId}";
        }

        public static string ResolveActivityStatus(NetworkRunner contextRunner, PlayerRef player)
        {
            NetworkObject playerObject = ResolvePlayerObject(contextRunner, player);
            if (playerObject != null && playerObject.TryGetComponent(out PlayerQuestStatusSync statusSync))
            {
                string status = statusSync.StatusText.ToString();
                if (!string.IsNullOrWhiteSpace(status))
                    return status;

                if (statusSync.LevelCompleted)
                    return PortfolioPlayerStatusFormatter.CompletedStatus;
            }

            return PortfolioPlayerStatusFormatter.DefaultExploringStatus;
        }

        private static NetworkObject GetPlayerObjectFromAuthoritativeRunner(NetworkRunner contextRunner, PlayerRef player)
        {
            foreach (NetworkRunner sessionRunner in FusionCoSessionRunners.Enumerate(contextRunner))
            {
                if (sessionRunner == null || !sessionRunner.IsRunning || sessionRunner.LocalPlayer != player)
                    continue;

                NetworkObject playerObject = sessionRunner.GetPlayerObject(player);
                if (playerObject != null)
                    return playerObject;
            }

            return null;
        }
    }
}
