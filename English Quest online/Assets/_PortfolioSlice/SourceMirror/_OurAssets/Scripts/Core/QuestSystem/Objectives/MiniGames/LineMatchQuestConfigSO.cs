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
    private static readonly MiniGameLifecycleContract Contract =
      new("line_match", requiresInteractionLock: true, supportsManualClose: true, publishesCompletionEvent: true);

    [SerializeField] private string gameId;
    [SerializeField] private LetterConnectionLevelConfigSO levelConfig;

    public LetterConnectionLevelConfigSO LevelConfig => levelConfig;
    public override string GameId => gameId;
    public override MiniGameLifecycleContract LifecycleContract => Contract;

    protected override bool TryValidateAuthoringInternal(out string error)
    {
      if (levelConfig == null)
      {
        error = "Line Match config is missing LevelConfig.";
        return false;
      }

      error = null;
      return true;
    }

    protected override bool TryValidateRuntimeInternal(MiniGameWorldLaunchHost host, out string error)
    {
      LetterConnectionBootstrap bootstrap = host.ResolveLineMatchBootstrap();
      if (bootstrap == null)
      {
        error = "LetterConnectionBootstrap not found on the launch host.";
        return false;
      }

      error = null;
      return true;
    }

    protected override bool TryLaunchInternal(MiniGameLaunchContext context)
    {
      LetterConnectionBootstrap bootstrap = context.Host.ResolveLineMatchBootstrap();
      return bootstrap.Open(
        levelConfig,
        context.Interactor,
        () => context.OnCompleted?.Invoke(0),
        context.OnClosed);
    }
  }
}

