using System.Collections.Generic;

namespace EnglishKingdom.QuestSystem
{
  public sealed class QuestMiniGameBinder : IQuestMiniGameBinder
  {
    private readonly Dictionary<string, QuestMiniGameConfigSO> _bindings = new();

    public bool TryGetConfig(string gameId, out QuestMiniGameConfigSO config)
    {
      if (string.IsNullOrEmpty(gameId))
      {
        config = null;
        return false;
      }

      return _bindings.TryGetValue(gameId, out config);
    }

    public void Refresh(IQuestService questService)
    {
      _bindings.Clear();
      if (questService == null)
        return;

      foreach (QuestInfo quest in questService.AllQuests)
      {
        if (quest == null || !quest.UsesObjectives() || quest.state != QuestState.IN_PROGRESS)
          continue;

        if (!quest.TryGetObjectiveDefinition(quest.currentStepIndex, out QuestObjectiveDefinition definition))
          continue;

        if (definition.type != QuestObjectiveType.CompleteMiniGame || string.IsNullOrEmpty(definition.targetId))
          continue;

        if (definition.miniGameConfig == null)
          continue;

        if (_bindings.ContainsKey(definition.targetId))
        {
          AppLog.Warning(
            $"[QuestMiniGameBinder] Duplicate active mini-game binding for '{definition.targetId}'. Keeping the first match.");
          continue;
        }

        _bindings[definition.targetId] = definition.miniGameConfig;
      }
    }
  }
}
