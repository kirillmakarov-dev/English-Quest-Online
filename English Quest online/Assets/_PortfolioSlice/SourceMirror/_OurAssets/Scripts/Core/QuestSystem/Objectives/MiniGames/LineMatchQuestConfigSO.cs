using System;
using Puzzle.Gameplay.MiniGames.LetterConnection;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
  [CreateAssetMenu(
    menuName = ScriptableObjectMenuPaths.CoreQuestMiniGameConfig + "/Line Match",
    fileName = "LineMatchQuestConfig_")]
  public class LineMatchQuestConfigSO : QuestMiniGameConfigSO
  {
    [SerializeField] private string gameId;
    [SerializeField] private LetterConnectionLevelConfigSO levelConfig;

    public LetterConnectionLevelConfigSO LevelConfig => levelConfig;
    public override string GameId => gameId;

    public override bool TryLaunch(
      MiniGameWorldLaunchHost host,
      PlayerInteraction interactor,
      Action<int> onCompleted,
      Action onClosed)
    {
      if (host == null || levelConfig == null)
        return false;

      LetterConnectionBootstrap bootstrap = host.ResolveLineMatchBootstrap();
      if (bootstrap == null)
      {
        AppLog.Warning("[LineMatchQuestConfigSO] LetterConnectionBootstrap not found.", host);
        return false;
      }

      bootstrap.Open(levelConfig, interactor, () => onCompleted?.Invoke(0), onClosed);
      return true;
    }
  }
}

