using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.QuestSystem
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
    public bool CanInteract => enabled && !string.IsNullOrEmpty(gameId) && HasActiveBinding();

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

      if (string.IsNullOrEmpty(gameId))
        AppLog.Warning($"[MiniGameWorldInteractable] '{name}' has an empty gameId.", this);

      if (launchHost == null)
        AppLog.Warning($"[MiniGameWorldInteractable] '{name}' is missing MiniGameWorldLaunchHost.", this);
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

      if (!config.TryValidateRuntime(launchHost, out string validationError))
      {
        AppLog.Warning(
          $"[MiniGameWorldInteractable] Mini-game '{gameId}' failed runtime validation: {validationError}",
          this);
        return false;
      }

      return config.TryLaunch(launchHost, interactor, OnGameCompleted, null);
    }

    public bool TryValidateCurrentBinding(out string error)
    {
      error = null;
      if (string.IsNullOrWhiteSpace(gameId))
      {
        error = "Mini-game station has an empty gameId.";
        return false;
      }

      if (launchHost == null)
      {
        error = "Mini-game station is missing MiniGameWorldLaunchHost.";
        return false;
      }

      if (!TryResolveConfig(out QuestMiniGameConfigSO config))
      {
        error = $"No active quest mini-game config found for '{gameId}'.";
        return false;
      }

      if (!config.TryValidateAuthoring(out error))
        return false;

      return launchHost.TryValidateBindings(config, out error);
    }

    private bool HasActiveBinding()
    {
      return ServiceLocator.For(this).TryGet(out IQuestMiniGameBinder binder) &&
             binder.TryGetConfig(gameId, out _);
    }

    private bool TryResolveConfig(out QuestMiniGameConfigSO config)
    {
      config = null;
      if (ServiceLocator.For(this).TryGet(out IQuestMiniGameBinder binder) &&
          binder.TryGetConfig(gameId, out config))
      {
        return true;
      }

      return false;
    }

    private void OnGameCompleted(int score)
    {
      AppLog.Info($"[MiniGameWorldInteractable] Completed '{gameId}' with score {score}. Publishing quest objective event.", this);
      QuestObjectiveEventBus.TryPublish(this, new QuestObjectiveEvents.MiniGameCompleted(gameId, score));
      CompleteActiveObjectiveFallback(score);
    }

    private void CompleteActiveObjectiveFallback(int score)
    {
      if (!ServiceLocator.For(this).TryGet(out IQuestService questService) || questService == null)
        return;

      foreach (QuestInfo quest in questService.AllQuests)
      {
        if (quest == null || quest.state != QuestState.IN_PROGRESS || !quest.UsesObjectives())
          continue;

        int stepIndex = quest.currentStepIndex;
        if (!quest.TryGetObjectiveDefinition(stepIndex, out QuestObjectiveDefinition definition))
          continue;

        if (definition.type != QuestObjectiveType.CompleteMiniGame || definition.targetId != gameId)
          continue;

        int minScore = definition.GetParameterInt("minScore", 0);
        if (minScore > 0 && score < minScore)
          continue;

        questService.CompleteObjectiveStep(quest, stepIndex, gameId);
        return;
      }
    }
  }
}

