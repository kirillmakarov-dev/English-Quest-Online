using System;
using Puzzle.Gameplay.MiniGames.DuolingoWordGame;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
  [CreateAssetMenu(
    menuName = ScriptableObjectMenuPaths.CoreQuestMiniGameConfig + "/Word Ordering",
    fileName = "WordOrderingQuestConfig_")]
  public class WordOrderingQuestConfigSO : QuestMiniGameConfigSO
  {
    [SerializeField] private string gameId;
    [SerializeField] private WordOrderingDataSO data;

    public WordOrderingDataSO Data => data;
    public override string GameId => gameId;

    public override bool TryLaunch(
      MiniGameWorldLaunchHost host,
      PlayerInteraction interactor,
      Action<int> onCompleted,
      Action onClosed)
    {
      if (host == null || data == null)
        return false;

      if (!host.TryGetWordGameMode(out WordOrderingMode mode))
      {
        AppLog.Warning("[WordOrderingQuestConfigSO] No WordOrderingMode found on launch host.", host);
        return false;
      }

      WordGameBootstrap bootstrap = host.ResolveWordGameBootstrap();
      if (bootstrap == null)
      {
        AppLog.Warning("[WordOrderingQuestConfigSO] WordGameBootstrap not found.", host);
        return false;
      }

      mode.SetData(data);
      bootstrap.Open(mode, interactor, () => onCompleted?.Invoke(0), onClosed);
      return true;
    }
  }
}

