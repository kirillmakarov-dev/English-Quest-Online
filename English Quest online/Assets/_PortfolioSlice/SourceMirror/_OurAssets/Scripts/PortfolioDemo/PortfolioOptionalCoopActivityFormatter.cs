namespace EnglishQuest.PortfolioDemo
{
    public readonly struct PortfolioOptionalCoopActivitySnapshot
    {
        public PortfolioOptionalCoopActivitySnapshot(
            string activityName,
            int connectedPlayers,
            int playersInside,
            int requiredPlayers,
            bool isLocalPlayerInside,
            bool isGroupActive)
        {
            ActivityName = string.IsNullOrWhiteSpace(activityName) ? "Study Circle" : activityName.Trim();
            ConnectedPlayers = connectedPlayers < 0 ? 0 : connectedPlayers;
            PlayersInside = playersInside < 0 ? 0 : playersInside;
            RequiredPlayers = requiredPlayers < 2 ? 2 : requiredPlayers;
            IsLocalPlayerInside = isLocalPlayerInside;
            IsGroupActive = isGroupActive;
        }

        public string ActivityName { get; }
        public int ConnectedPlayers { get; }
        public int PlayersInside { get; }
        public int RequiredPlayers { get; }
        public bool IsLocalPlayerInside { get; }
        public bool IsGroupActive { get; }
    }

    public static class PortfolioOptionalCoopActivityFormatter
    {
        public static string AppendOptionalCoopStatus(
            string baseStatus,
            PortfolioOptionalCoopActivitySnapshot snapshot)
        {
            string status = string.IsNullOrWhiteSpace(baseStatus)
                ? PortfolioPlayerStatusFormatter.DefaultExploringStatus
                : baseStatus.Trim();

            string suffix = BuildPlayerStatusSuffix(snapshot);
            return string.IsNullOrWhiteSpace(suffix)
                ? status
                : $"{status} | {suffix}";
        }

        public static string BuildPlayerStatusSuffix(PortfolioOptionalCoopActivitySnapshot snapshot)
        {
            if (snapshot.ConnectedPlayers <= 1 || !snapshot.IsLocalPlayerInside)
                return string.Empty;

            if (snapshot.IsGroupActive)
                return $"{snapshot.ActivityName} active";

            return $"Waiting at {snapshot.ActivityName} ({snapshot.PlayersInside}/{snapshot.RequiredPlayers})";
        }

        public static string BuildBriefingLine(PortfolioOptionalCoopActivitySnapshot snapshot)
        {
            if (snapshot.ConnectedPlayers <= 1)
            {
                return $"Optional co-op only: if a second player joins later, stand together in the {snapshot.ActivityName} to trigger a shared multiplayer moment. This never blocks quest progress.";
            }

            if (snapshot.IsGroupActive)
                return $"Optional co-op active: both players are inside the {snapshot.ActivityName}. Quest progression still remains per-player.";

            if (snapshot.PlayersInside > 0)
            {
                return $"Optional co-op: {snapshot.PlayersInside}/{snapshot.RequiredPlayers} players are inside the {snapshot.ActivityName}. Quest progression is unaffected.";
            }

            return $"Optional co-op only: stand together in the {snapshot.ActivityName} to trigger a shared multiplayer moment. It does not unlock or block missions.";
        }

        public static string BuildDebugLine(PortfolioOptionalCoopActivitySnapshot snapshot)
        {
            string state = snapshot.IsGroupActive
                ? "ACTIVE"
                : snapshot.PlayersInside > 0
                    ? "WAITING"
                    : "IDLE";

            return $"Optional Co-op: {snapshot.ActivityName} - {state} ({snapshot.PlayersInside}/{snapshot.RequiredPlayers})";
        }
    }
}
