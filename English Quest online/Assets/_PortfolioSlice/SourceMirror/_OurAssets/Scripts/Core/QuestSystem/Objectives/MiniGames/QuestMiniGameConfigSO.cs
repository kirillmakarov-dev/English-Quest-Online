using System;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
  public readonly struct MiniGameLaunchContext
  {
    public MiniGameLaunchContext(
      MiniGameWorldLaunchHost host,
      PlayerInteraction interactor,
      Action<int> onCompleted,
      Action onClosed)
    {
      Host = host;
      Interactor = interactor;
      OnCompleted = onCompleted;
      OnClosed = onClosed;
    }

    public MiniGameWorldLaunchHost Host { get; }
    public PlayerInteraction Interactor { get; }
    public Action<int> OnCompleted { get; }
    public Action OnClosed { get; }
  }

  public readonly struct MiniGameLifecycleContract
  {
    public MiniGameLifecycleContract(
      string typeId,
      bool requiresInteractionLock,
      bool supportsManualClose,
      bool publishesCompletionEvent)
    {
      TypeId = typeId;
      RequiresInteractionLock = requiresInteractionLock;
      SupportsManualClose = supportsManualClose;
      PublishesCompletionEvent = publishesCompletionEvent;
    }

    public string TypeId { get; }
    public bool RequiresInteractionLock { get; }
    public bool SupportsManualClose { get; }
    public bool PublishesCompletionEvent { get; }
  }

  /// <summary>
  /// Shared runtime contract for quest-owned mini-games.
  /// A config must validate its authoring data, validate runtime bindings on the launch host,
  /// and expose the same lifecycle guarantees regardless of the specific mini-game implementation.
  /// </summary>
  public interface IQuestMiniGameLifecycle
  {
    string GameId { get; }
    MiniGameLifecycleContract LifecycleContract { get; }
    bool TryValidateAuthoring(out string error);
    bool TryValidateRuntime(MiniGameWorldLaunchHost host, out string error);
    bool TryLaunch(MiniGameLaunchContext context);
  }

  /// <summary>
  /// Quest-owned wrapper that binds a mini-game catalog id to launchable content.
  /// </summary>
  public abstract class QuestMiniGameConfigSO : ScriptableObject, IQuestMiniGameLifecycle
  {
    public abstract string GameId { get; }
    public abstract MiniGameLifecycleContract LifecycleContract { get; }

    public bool TryValidateAuthoring(out string error)
    {
      if (string.IsNullOrWhiteSpace(GameId))
      {
        error = $"{GetType().Name} has an empty GameId.";
        return false;
      }

      if (string.IsNullOrWhiteSpace(LifecycleContract.TypeId))
      {
        error = $"{GetType().Name} has an empty lifecycle TypeId.";
        return false;
      }

      return TryValidateAuthoringInternal(out error);
    }

    public bool TryValidateRuntime(MiniGameWorldLaunchHost host, out string error)
    {
      if (host == null)
      {
        error = $"{GetType().Name} requires a MiniGameWorldLaunchHost.";
        return false;
      }

      return TryValidateRuntimeInternal(host, out error);
    }

    public bool TryLaunch(MiniGameLaunchContext context)
    {
      if (!TryValidateAuthoring(out string authoringError))
      {
        AppLog.Warning($"[{GetType().Name}] Authoring validation failed: {authoringError}", this);
        return false;
      }

      if (!TryValidateRuntime(context.Host, out string runtimeError))
      {
        AppLog.Warning($"[{GetType().Name}] Runtime validation failed: {runtimeError}", context.Host);
        return false;
      }

      return TryLaunchInternal(context);
    }

    public bool TryLaunch(
      MiniGameWorldLaunchHost host,
      PlayerInteraction interactor,
      Action<int> onCompleted,
      Action onClosed)
    {
      return TryLaunch(new MiniGameLaunchContext(host, interactor, onCompleted, onClosed));
    }

    protected abstract bool TryValidateAuthoringInternal(out string error);
    protected abstract bool TryValidateRuntimeInternal(MiniGameWorldLaunchHost host, out string error);
    protected abstract bool TryLaunchInternal(MiniGameLaunchContext context);
  }
}

