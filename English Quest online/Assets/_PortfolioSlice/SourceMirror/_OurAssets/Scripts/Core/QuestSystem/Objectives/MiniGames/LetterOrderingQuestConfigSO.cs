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
    private static readonly MiniGameLifecycleContract Contract =
      new("letter_ordering", requiresInteractionLock: true, supportsManualClose: true, publishesCompletionEvent: true);

    [SerializeField] private string gameId;
    [SerializeField] private LetterOrderingDataSO data;

    public LetterOrderingDataSO Data => data;
    public override string GameId => gameId;
    public override MiniGameLifecycleContract LifecycleContract => Contract;

    protected override bool TryValidateAuthoringInternal(out string error)
    {
      if (data == null)
      {
        error = "Letter Ordering config is missing Data.";
        return false;
      }

      error = null;
      return true;
    }

    protected override bool TryValidateRuntimeInternal(MiniGameWorldLaunchHost host, out string error)
    {
      if (!host.TryGetWordGameMode(out LetterOrderingMode mode))
      {
        error = "LetterOrderingMode not found on the launch host.";
        return false;
      }

      WordGameBootstrap bootstrap = host.ResolveWordGameBootstrap();
      if (bootstrap == null)
      {
        error = "WordGameBootstrap not found on the launch host.";
        return false;
      }

      error = null;
      return true;
    }

    protected override bool TryLaunchInternal(MiniGameLaunchContext context)
    {
      context.Host.TryGetWordGameMode(out LetterOrderingMode mode);
      WordGameBootstrap bootstrap = context.Host.ResolveWordGameBootstrap();
      if (mode == null || bootstrap == null)
        return false;

      mode.SetData(data);
      bootstrap.Open(mode, context.Interactor, () => context.OnCompleted?.Invoke(0), context.OnClosed);
      return true;
    }
  }
}

