using System;

namespace EnglishKingdom.QuestSystem
{
    public static class QuestObjectiveEvents
    {
        public readonly struct NpcInteracted
        {
            public string NpcId { get; }
            public NpcInteracted(string npcId) => NpcId = npcId;
        }

        public readonly struct AreaEntered
        {
            public string AreaId { get; }
            public AreaEntered(string areaId) => AreaId = areaId;
        }

        public readonly struct ItemCollected
        {
            public string ItemId { get; }
            public ItemCollected(string itemId) => ItemId = itemId;
        }

        public readonly struct MiniGameCompleted
        {
            public string GameId { get; }
            public int Score { get; }
            public MiniGameCompleted(string gameId, int score = 0)
            {
                GameId = gameId;
                Score = score;
            }
        }

        public readonly struct CustomObjectiveSignaled
        {
            public string HandlerId { get; }
            public string Payload { get; }
            public CustomObjectiveSignaled(string handlerId, string payload = "")
            {
                HandlerId = handlerId;
                Payload = payload;
            }
        }
    }

    public interface IQuestObjectiveEventBus
    {
        event Action<QuestObjectiveEvents.NpcInteracted> OnNpcInteracted;
        event Action<QuestObjectiveEvents.AreaEntered> OnAreaEntered;
        event Action<QuestObjectiveEvents.ItemCollected> OnItemCollected;
        event Action<QuestObjectiveEvents.MiniGameCompleted> OnMiniGameCompleted;
        event Action<QuestObjectiveEvents.CustomObjectiveSignaled> OnCustomObjectiveSignaled;

        void Publish(QuestObjectiveEvents.NpcInteracted e);
        void Publish(QuestObjectiveEvents.AreaEntered e);
        void Publish(QuestObjectiveEvents.ItemCollected e);
        void Publish(QuestObjectiveEvents.MiniGameCompleted e);
        void Publish(QuestObjectiveEvents.CustomObjectiveSignaled e);
    }
}
