using System;
using UnityEngine;

namespace EnglishKingdom.QuestSystem
{
  /// <summary>
  /// Quest-owned wrapper that binds a mini-game catalog id to launchable content.
  /// </summary>
  public abstract class QuestMiniGameConfigSO : ScriptableObject
  {
    public abstract string GameId { get; }

    public abstract bool TryLaunch(
      MiniGameWorldLaunchHost host,
      PlayerInteraction interactor,
      Action<int> onCompleted,
      Action onClosed);
  }
}
