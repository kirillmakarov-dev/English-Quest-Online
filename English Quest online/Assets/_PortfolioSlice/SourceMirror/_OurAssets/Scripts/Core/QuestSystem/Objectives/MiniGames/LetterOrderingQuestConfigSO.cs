using System;
using Puzzle.Gameplay.MiniGames.DuolingoWordGame;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
  [CreateAssetMenu(
    menuName = ScriptableObjectMenuPaths.CoreQuestMiniGameConfig + "/Letter Ordering",
    fileName = "LetterOrderingQuestConfig_")]
  public class LetterOrderingQuestConfigSO : QuestMiniGameConfigSO
  {
    [SerializeField] private string gameId;
    [SerializeField] private LetterOrderingDataSO data;

    public LetterOrderingDataSO Data => data;
    public override string GameId => gameId;

    public override bool TryLaunch(
      MiniGameWorldLaunchHost host,
      PlayerInteraction interactor,
      Action<int> onCompleted,
      Action onClosed)
    {
      if (host == null || data == null)
        return false;

      if (!host.TryGetWordGameMode(out LetterOrderingMode mode))
      {
        AppLog.Warning("[LetterOrderingQuestConfigSO] No LetterOrderingMode found on launch host.", host);
        return false;
      }

      WordGameBootstrap bootstrap = host.ResolveWordGameBootstrap();
      if (bootstrap == null)
      {
        AppLog.Warning("[LetterOrderingQuestConfigSO] WordGameBootstrap not found.", host);
        return false;
      }

      mode.SetData(data);
      bootstrap.Open(mode, interactor, () => onCompleted?.Invoke(0), onClosed);
      return true;
    }
  }
}

