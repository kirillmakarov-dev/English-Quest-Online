using UnityEngine;

namespace EnglishKingdom.QuestSystem
{
    public interface IQuestWorldTargetRegistry
    {
        void Register(QuestObjectiveType type, string targetId, Transform transform);
        void Unregister(QuestObjectiveType type, string targetId, Transform transform);
        bool TryGetTransform(QuestObjectiveType type, string targetId, out Transform transform);
    }
}
