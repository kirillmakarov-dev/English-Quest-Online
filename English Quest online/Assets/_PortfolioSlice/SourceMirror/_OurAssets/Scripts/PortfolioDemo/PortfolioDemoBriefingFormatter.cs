using EnglishQuest.QuestSystem;

namespace EnglishQuest.PortfolioDemo
{
    public readonly struct PortfolioDemoBriefingContent
    {
        public PortfolioDemoBriefingContent(string title, string body)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
        }

        public string Title { get; }
        public string Body { get; }
    }

    public static class PortfolioDemoBriefingFormatter
    {
        private const string DefaultHeadline = "Lesson";
        private const string DefaultNpcName = "the active NPC";
        private const string DefaultObjective = "Complete the active lesson objective.";

        public static PortfolioDemoBriefingContent Build(
            bool isSessionReady,
            bool isLevelCompleted,
            int connectedPlayerCount,
            QuestState? highlightedQuestState,
            string questHeadline,
            string npcDisplayName,
            string objectiveSummary,
            string optionalCoopLine = null)
        {
            if (!isSessionReady)
            {
                return new PortfolioDemoBriefingContent(
                    "Connecting",
                    "Preparing the local player and starting the shared session.\nSolo play is fully supported once the player spawns, and any second player remains optional.");
            }

            if (isLevelCompleted)
            {
                return new PortfolioDemoBriefingContent(
                    "Prototype Complete",
                    "You finished the full learning slice.\nSolo completion remains fully valid in this MVP, and any second player stays optional.\nUse the completion panel to replay from the beginning or close it and walk the space again.");
            }

            string headline = SanitizeHeadline(questHeadline);
            string npcName = SanitizeNpcName(npcDisplayName);
            string objective = SanitizeObjective(objectiveSummary);
            bool hasOtherPlayers = connectedPlayerCount > 1;

            return highlightedQuestState switch
            {
                QuestState.IN_PROGRESS => new PortfolioDemoBriefingContent(
                    $"Current: {headline}",
                    AppendOptionalCoopLine(
                    hasOtherPlayers
                        ? $"Current objective: {objective}\nIf another player is in the room, they can keep progressing independently on their own lesson state."
                        : $"Current objective: {objective}\nSolo play is active. A second player is optional and does not block this lesson.",
                    optionalCoopLine)),

                QuestState.CAN_FINISH => new PortfolioDemoBriefingContent(
                    $"Return to {npcName}",
                    AppendOptionalCoopLine(
                        $"Lesson complete. Talk to {npcName} to close this stage and unlock the next one.",
                        optionalCoopLine)),

                QuestState.CAN_START => new PortfolioDemoBriefingContent(
                    $"Next: {headline}",
                    AppendOptionalCoopLine(
                    hasOtherPlayers
                        ? $"Talk to {npcName} to begin.\nLearning goal: {objective}\nOther players can stay on their own lesson state while you start this one."
                        : $"Talk to {npcName} to begin.\nLearning goal: {objective}\nSolo play is fully supported for this lesson.",
                    optionalCoopLine)),

                QuestState.REQUIREMENTS_NOT_MET => new PortfolioDemoBriefingContent(
                    $"Locked: {headline}",
                    AppendOptionalCoopLine(
                        "Finish the earlier lesson chain first. The next unlock will happen automatically once the current required lesson is completed.\nThis lock is about quest order, not player count.",
                        optionalCoopLine)),

                _ => new PortfolioDemoBriefingContent(
                    "Explore the Demo",
                    AppendOptionalCoopLine(
                        "This slice demonstrates open-world quests, educational mini-games, and optional 2-player Photon Fusion presence. Solo completion remains the primary flow.",
                        optionalCoopLine))
            };
        }

        private static string SanitizeHeadline(string questHeadline)
        {
            return string.IsNullOrWhiteSpace(questHeadline)
                ? DefaultHeadline
                : questHeadline.Trim();
        }

        private static string SanitizeNpcName(string npcDisplayName)
        {
            return string.IsNullOrWhiteSpace(npcDisplayName)
                ? DefaultNpcName
                : npcDisplayName.Trim();
        }

        private static string SanitizeObjective(string objectiveSummary)
        {
            return string.IsNullOrWhiteSpace(objectiveSummary)
                ? DefaultObjective
                : objectiveSummary.Trim();
        }

        private static string AppendOptionalCoopLine(string baseBody, string optionalCoopLine)
        {
            if (string.IsNullOrWhiteSpace(optionalCoopLine))
                return baseBody;

            return $"{baseBody}\n{optionalCoopLine.Trim()}";
        }
    }
}
