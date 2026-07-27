using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
    [Serializable]
    public class NpcCatalogEntry
    {
        public string id;
        public string displayName;
    }

    [CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestNpcCatalog, fileName = "NpcCatalog_")]
    public class NpcCatalogSO : ScriptableObject
    {
        [SerializeField] private List<NpcCatalogEntry> entries = new();

        public IReadOnlyList<NpcCatalogEntry> Entries => entries;

        public bool TryGetById(string npcId, out NpcCatalogEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(npcId))
                return false;

            foreach (NpcCatalogEntry e in entries)
            {
                if (e != null && e.id == npcId)
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
            foreach (NpcCatalogEntry e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.id))
                    continue;

                if (!seen.Add(e.id))
                    Debug.LogWarning($"[NpcCatalogSO] Duplicate npc id '{e.id}' in '{name}'.", this);
            }
        }
#endif
    }
}

