using System.Collections.Generic;
using System.Text;

namespace EnglishQuest.PortfolioDemo
{
    public readonly struct PortfolioDebugLocalPlayerSnapshot
    {
        public PortfolioDebugLocalPlayerSnapshot(
            string objectName,
            bool ownsPlayer,
            bool drivesView,
            int stateAuthorityPlayerId,
            int inputAuthorityPlayerId,
            bool isMissing = false)
        {
            ObjectName = objectName ?? string.Empty;
            OwnsPlayer = ownsPlayer;
            DrivesView = drivesView;
            StateAuthorityPlayerId = stateAuthorityPlayerId;
            InputAuthorityPlayerId = inputAuthorityPlayerId;
            IsMissing = isMissing;
        }

        public string ObjectName { get; }
        public bool OwnsPlayer { get; }
        public bool DrivesView { get; }
        public int StateAuthorityPlayerId { get; }
        public int InputAuthorityPlayerId { get; }
        public bool IsMissing { get; }
    }

    public readonly struct PortfolioDebugSessionPlayerSnapshot
    {
        public PortfolioDebugSessionPlayerSnapshot(string playerName, string activity)
        {
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Unknown Player" : playerName.Trim();
            Activity = string.IsNullOrWhiteSpace(activity) ? "Unknown activity" : activity.Trim();
        }

        public string PlayerName { get; }
        public string Activity { get; }
    }

    public static class PortfolioDemoDebugOverlayFormatter
    {
        public static string FormatLocalPlayerSection(PortfolioDebugLocalPlayerSnapshot snapshot)
        {
            if (snapshot.IsMissing)
                return "Local Object: Missing";

            var builder = new StringBuilder(160);
            builder.AppendLine($"Local Object: {snapshot.ObjectName}");
            builder.AppendLine($"Owns Player: {snapshot.OwnsPlayer}");
            builder.AppendLine($"Drives View: {snapshot.DrivesView}");
            builder.AppendLine($"State Authority: {snapshot.StateAuthorityPlayerId}");
            builder.Append($"Input Authority: {snapshot.InputAuthorityPlayerId}");
            return builder.ToString();
        }

        public static string FormatSessionPlayersSection(
            IReadOnlyList<PortfolioDebugSessionPlayerSnapshot> players,
            bool isSoloSession)
        {
            var builder = new StringBuilder(256);
            string topologyLabel = PortfolioPlayerStatusFormatter.GetSessionTopologyLabel(
                isSoloSession ? 1 : 2);
            builder.AppendLine($"Session Players: {topologyLabel}");

            if (players == null || players.Count == 0)
            {
                builder.Append(isSoloSession
                    ? "- Local player is preparing"
                    : "- None");
                return builder.ToString();
            }

            for (int i = 0; i < players.Count; i++)
            {
                PortfolioDebugSessionPlayerSnapshot player = players[i];
                builder.AppendLine($"- {player.PlayerName}: {player.Activity}");
            }

            if (builder.Length >= System.Environment.NewLine.Length)
                builder.Length -= System.Environment.NewLine.Length;

            return builder.ToString();
        }

        public static string AppendFlowState(string baseActivity, PortfolioGameFlowState flowState)
        {
            string activity = string.IsNullOrWhiteSpace(baseActivity)
                ? "Exploring open world"
                : baseActivity.Trim();

            return $"{activity} [{flowState}]";
        }

        public static string FormatOwnershipRulesSection()
        {
            var builder = new StringBuilder(256);
            builder.AppendLine("Ownership Rules:");
            builder.AppendLine("- Dialogue: local player only");
            builder.AppendLine("- Mini-games: local player only");
            builder.AppendLine("- Quest progress: per-player");
            builder.AppendLine("- Optional co-op: shared world only");
            builder.AppendLine("- Main lesson chain: fully solo-playable");
            builder.Append("- Second player: never required");
            return builder.ToString();
        }
    }
}
