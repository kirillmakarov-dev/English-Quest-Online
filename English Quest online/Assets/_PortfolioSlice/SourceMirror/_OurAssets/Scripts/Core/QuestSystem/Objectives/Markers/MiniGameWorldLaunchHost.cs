using Puzzle.Gameplay.MiniGames.DuolingoWordGame;
using Puzzle.Gameplay.MiniGames.LetterConnection;
using UnityEngine;

namespace EnglishKingdom.QuestSystem
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
  }
}
