using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.QuestSystem
{
    [DefaultExecutionOrder(5)]
    [AddComponentMenu(QuestSystemComponentMenuPaths.Objectives + "/Quest Objective Director")]
    public class QuestObjectiveDirector : MonoBehaviour
    {
        private QuestObjectiveHandlerRegistry _registry;
        private IQuestObjectiveEventBus _eventBus;

        public void Initialize(IQuestWorldResolver resolver, IQuestService questService)
        {
            if (resolver == null || questService == null)
                return;

            _registry = new QuestObjectiveHandlerRegistry(resolver, questService);
            TrySubscribe();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void TrySubscribe()
        {
            if (_registry == null)
                return;

            if (_eventBus == null && ServiceLocator.For(this).TryGet(out _eventBus))
            {
                _eventBus.OnNpcInteracted += HandleNpcInteracted;
                _eventBus.OnAreaEntered += HandleAreaEntered;
                _eventBus.OnItemCollected += HandleItemCollected;
                _eventBus.OnMiniGameCompleted += HandleMiniGameCompleted;
                _eventBus.OnCustomObjectiveSignaled += HandleCustomObjective;
            }
        }

        private void Unsubscribe()
        {
            if (_eventBus == null)
                return;

            _eventBus.OnNpcInteracted -= HandleNpcInteracted;
            _eventBus.OnAreaEntered -= HandleAreaEntered;
            _eventBus.OnItemCollected -= HandleItemCollected;
            _eventBus.OnMiniGameCompleted -= HandleMiniGameCompleted;
            _eventBus.OnCustomObjectiveSignaled -= HandleCustomObjective;
            _eventBus = null;
        }

        private void HandleNpcInteracted(QuestObjectiveEvents.NpcInteracted e) => _registry?.HandleNpcInteracted(e);
        private void HandleAreaEntered(QuestObjectiveEvents.AreaEntered e) => _registry?.HandleAreaEntered(e);
        private void HandleItemCollected(QuestObjectiveEvents.ItemCollected e) => _registry?.HandleItemCollected(e);
        private void HandleMiniGameCompleted(QuestObjectiveEvents.MiniGameCompleted e) => _registry?.HandleMiniGameCompleted(e);
        private void HandleCustomObjective(QuestObjectiveEvents.CustomObjectiveSignaled e) => _registry?.HandleCustomObjective(e);
    }
}

