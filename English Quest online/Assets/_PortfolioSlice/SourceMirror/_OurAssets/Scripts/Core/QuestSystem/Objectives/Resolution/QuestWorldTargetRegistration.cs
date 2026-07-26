using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.QuestSystem
{
    public static class QuestWorldTargetRegistration
    {
        public static void TryRegister(MonoBehaviour source, QuestObjectiveType type, string targetId, Transform transform = null)
        {
            if (source == null || string.IsNullOrEmpty(targetId))
                return;

            Transform resolved = transform != null ? transform : source.transform;
            ServiceLocator locator = ServiceLocator.For(source);
            if (locator == null || !locator.TryGet(out IQuestWorldTargetRegistry registry))
                return;

            registry.Register(type, targetId, resolved);
        }

        public static void TryUnregister(MonoBehaviour source, QuestObjectiveType type, string targetId, Transform transform = null)
        {
            if (source == null || string.IsNullOrEmpty(targetId))
                return;

            Transform resolved = transform != null ? transform : source.transform;
            ServiceLocator locator = ServiceLocator.For(source);
            if (locator == null || !locator.TryGet(out IQuestWorldTargetRegistry registry))
                return;

            registry.Unregister(type, targetId, resolved);
        }
    }
}
