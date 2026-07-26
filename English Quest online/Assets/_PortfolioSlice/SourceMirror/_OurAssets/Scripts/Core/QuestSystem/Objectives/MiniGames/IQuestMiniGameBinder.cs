namespace EnglishKingdom.QuestSystem
{
  public interface IQuestMiniGameBinder
  {
    bool TryGetConfig(string gameId, out QuestMiniGameConfigSO config);
    void Refresh(IQuestService questService);
  }
}
