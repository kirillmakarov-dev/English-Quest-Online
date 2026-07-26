using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.QuestSystem
{
  /// <summary>
  /// Generic world interactable that launches a quest-owned mini-game config and publishes completion by id.
  /// </summary>
  [AddComponentMenu(QuestSystemComponentMenuPaths.Markers + "/Mini Game Interactable")]
  public class MiniGameWorldInteractable : MonoBehaviour, IInteractable
  {
    [SerializeField] private string gameId;
    [SerializeField] private string interactionPrompt = "Play";
    [SerializeField] private MiniGameWorldLaunchHost launchHost;

    public string GameId => gameId;
    public string InteractionPrompt => interactionPrompt;
    public bool CanInteract => enabled && !string.IsNullOrEmpty(gameId);

    private void OnEnable()
    {
      QuestWorldTargetRegistration.TryRegister(this, QuestObjectiveType.CompleteMiniGame, gameId);
    }

    private void OnDisable()
    {
      QuestWorldTargetRegistration.TryUnregister(this, QuestObjectiveType.CompleteMiniGame, gameId);
    }

    private void Awake()
    {
      if (launchHost == null)
        launchHost = GetComponent<MiniGameWorldLaunchHost>();
    }

    public bool Interact(PlayerInteraction interactor)
    {
      if (!CanInteract)
        return false;

      if (launchHost == null)
      {
        AppLog.Warning($"[MiniGameWorldInteractable] Missing MiniGameWorldLaunchHost on '{name}'.", this);
        return false;
      }

      if (!TryResolveConfig(out QuestMiniGameConfigSO config))
      {
        AppLog.Warning($"[MiniGameWorldInteractable] No quest mini-game config found for '{gameId}'.", this);
        return false;
      }

      return config.TryLaunch(launchHost, interactor, OnGameCompleted, null);
    }

    private bool TryResolveConfig(out QuestMiniGameConfigSO config)
    {
      config = null;
      if (ServiceLocator.For(this).TryGet(out IQuestMiniGameBinder binder) &&
          binder.TryGetConfig(gameId, out config))
      {
        return true;
      }

      QuestMiniGameConfigSO fallback = launchHost.FallbackConfig;
      if (fallback != null && fallback.GameId == gameId)
      {
        config = fallback;
        return true;
      }

      return false;
    }

    private void OnGameCompleted(int score)
    {
      QuestObjectiveEventBus.TryPublish(this, new QuestObjectiveEvents.MiniGameCompleted(gameId, score));
    }
  }
}
