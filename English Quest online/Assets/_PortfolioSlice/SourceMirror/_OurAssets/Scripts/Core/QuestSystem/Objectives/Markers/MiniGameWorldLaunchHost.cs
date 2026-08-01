using Puzzle.Gameplay.MiniGames.DuolingoWordGame;
using Puzzle.Gameplay.MiniGames.LetterConnection;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
  /// <summary>
  /// Scene bootstrap references for <see cref="MiniGameWorldInteractable"/>.
  /// Place on the same GameObject as the interactable marker.
  /// </summary>
  [AddComponentMenu(QuestSystemComponentMenuPaths.Markers + "/Mini Game Launch Host")]
  public class MiniGameWorldLaunchHost : MonoBehaviour
  {
    [SerializeField] private WordGameBootstrap wordGameBootstrap;
    [SerializeField] private LetterConnectionBootstrap lineMatchBootstrap;

    [Tooltip("Optional config used when no active quest binding exists (debug / non-quest play).")]
    [SerializeField] private QuestMiniGameConfigSO fallbackConfig;

    public QuestMiniGameConfigSO FallbackConfig => fallbackConfig;

    public bool TryValidateSetup(out string error)
    {
      error = null;

      if (wordGameBootstrap == null && lineMatchBootstrap == null && fallbackConfig == null)
      {
        error = $"Mini-game launch host '{name}' has no bootstrap references and no fallback config.";
        return false;
      }

      if (fallbackConfig != null && !fallbackConfig.TryValidateAuthoring(out error))
        return false;

      return true;
    }

    public void CollectConfigurationIssues(List<string> errors, List<string> warnings)
    {
      errors ??= new List<string>();
      warnings ??= new List<string>();

      if (!TryValidateSetup(out string error))
        errors.Add(error);
    }

    private void Awake()
    {
      if (wordGameBootstrap == null && lineMatchBootstrap == null)
      {
        AppLog.Warning(
          $"[MiniGameWorldLaunchHost] '{name}' has no bootstrap references. It can only launch through fallback discovery.",
          this);
      }
    }

    public WordGameBootstrap ResolveWordGameBootstrap()
    {
      if (wordGameBootstrap != null)
        return wordGameBootstrap;

      return FindFirstObjectByType<WordGameBootstrap>(FindObjectsInactive.Include);
    }

    public LetterConnectionBootstrap ResolveLineMatchBootstrap()
    {
      if (lineMatchBootstrap != null)
        return lineMatchBootstrap;

      return FindFirstObjectByType<LetterConnectionBootstrap>(FindObjectsInactive.Include);
    }

    public bool TryGetWordGameMode<T>(out T mode) where T : class, IWordGameMode
    {
      mode = GetComponent<T>();
      if (mode != null)
        return true;

      WordGameBootstrap bootstrap = ResolveWordGameBootstrap();
      if (bootstrap != null)
      {
        mode = bootstrap.GetComponent<T>();
        if (mode != null)
          return true;
      }

      return false;
    }

    public bool TryValidateBindings(QuestMiniGameConfigSO config, out string error)
    {
      if (config == null)
      {
        error = "QuestMiniGameConfigSO is not assigned.";
        return false;
      }

      return config.TryValidateRuntime(this, out error);
    }
  }
}

