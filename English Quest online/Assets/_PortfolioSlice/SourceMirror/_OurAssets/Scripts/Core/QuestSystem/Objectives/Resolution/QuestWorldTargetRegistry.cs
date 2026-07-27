using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
    public sealed class QuestWorldTargetRegistry : IQuestWorldTargetRegistry
    {
        readonly Dictionary<(QuestObjectiveType Type, string TargetId), Transform> _targets = new();

        public void Register(QuestObjectiveType type, string targetId, Transform transform)
        {
            if (string.IsNullOrEmpty(targetId) || transform == null)
                return;

            _targets[(type, targetId)] = transform;
        }

        public void Unregister(QuestObjectiveType type, string targetId, Transform transform)
        {
            if (string.IsNullOrEmpty(targetId))
                return;

            var key = (type, targetId);
            if (_targets.TryGetValue(key, out Transform current) && current == transform)
                _targets.Remove(key);
        }

        public bool TryGetTransform(QuestObjectiveType type, string targetId, out Transform transform)
        {
            transform = null;
            if (string.IsNullOrEmpty(targetId))
                return false;

            if (!_targets.TryGetValue((type, targetId), out transform))
                return false;

            if (transform == null)
            {
                _targets.Remove((type, targetId));
                return false;
            }

            return true;
        }
    }
}

