using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
    public enum InteractableCatalogKind
    {
        Collectible,
        MiniGame,
        Custom
    }

    [Serializable]
    public class InteractableCatalogEntry
    {
        public string id;
        public InteractableCatalogKind kind = InteractableCatalogKind.Collectible;
        public string displayName;
    }

    [CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestInteractableCatalog, fileName = "InteractableCatalog_")]
    public class InteractableCatalogSO : ScriptableObject
    {
        [SerializeField] private List<InteractableCatalogEntry> entries = new();

        public IReadOnlyList<InteractableCatalogEntry> Entries => entries;

        public bool TryGetById(string interactableId, out InteractableCatalogEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(interactableId))
                return false;

            foreach (InteractableCatalogEntry e in entries)
            {
                if (e != null && e.id == interactableId)
                {
                    entry = e;
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (InteractableCatalogEntry e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.id))
                    continue;

                if (!seen.Add(e.id))
                    Debug.LogWarning($"[InteractableCatalogSO] Duplicate interactable id '{e.id}' in '{name}'.", this);
            }
        }
#endif
    }
}

