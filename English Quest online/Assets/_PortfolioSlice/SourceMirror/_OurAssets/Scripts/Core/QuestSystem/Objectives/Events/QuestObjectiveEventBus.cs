using System;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.QuestSystem
{
    [DefaultExecutionOrder(-5)]
    [AddComponentMenu(QuestSystemComponentMenuPaths.Objectives + "/Quest Objective Event Bus")]
    public class QuestObjectiveEventBus : MonoBehaviour, IQuestObjectiveEventBus
    {
        public event Action<QuestObjectiveEvents.NpcInteracted> OnNpcInteracted;
        public event Action<QuestObjectiveEvents.AreaEntered> OnAreaEntered;
        public event Action<QuestObjectiveEvents.ItemCollected> OnItemCollected;
        public event Action<QuestObjectiveEvents.MiniGameCompleted> OnMiniGameCompleted;
        public event Action<QuestObjectiveEvents.CustomObjectiveSignaled> OnCustomObjectiveSignaled;

        private void Awake()
        {
            ServiceLocator.For(this).Register<IQuestObjectiveEventBus>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.DeregisterFor<IQuestObjectiveEventBus>(this);
        }

        public void Publish(QuestObjectiveEvents.NpcInteracted e) => OnNpcInteracted?.Invoke(e);
        public void Publish(QuestObjectiveEvents.AreaEntered e) => OnAreaEntered?.Invoke(e);
        public void Publish(QuestObjectiveEvents.ItemCollected e) => OnItemCollected?.Invoke(e);
        public void Publish(QuestObjectiveEvents.MiniGameCompleted e) => OnMiniGameCompleted?.Invoke(e);
        public void Publish(QuestObjectiveEvents.CustomObjectiveSignaled e) => OnCustomObjectiveSignaled?.Invoke(e);

        public static void TryPublish(MonoBehaviour context, QuestObjectiveEvents.NpcInteracted e)
        {
            if (context != null && ServiceLocator.For(context).TryGet<IQuestObjectiveEventBus>(out var bus))
                bus.Publish(e);
        }

        public static void TryPublish(MonoBehaviour context, QuestObjectiveEvents.AreaEntered e)
        {
            if (context != null && ServiceLocator.For(context).TryGet<IQuestObjectiveEventBus>(out var bus))
                bus.Publish(e);
        }

        public static void TryPublish(MonoBehaviour context, QuestObjectiveEvents.ItemCollected e)
        {
            if (context != null && ServiceLocator.For(context).TryGet<IQuestObjectiveEventBus>(out var bus))
                bus.Publish(e);
        }

        public static void TryPublish(MonoBehaviour context, QuestObjectiveEvents.MiniGameCompleted e)
        {
            if (context != null && ServiceLocator.For(context).TryGet<IQuestObjectiveEventBus>(out var bus))
                bus.Publish(e);
        }
    }
}
