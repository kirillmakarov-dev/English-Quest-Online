using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishKingdom.QuestSystem
{
    [Serializable]
    public class AreaCatalogEntry
    {
        public string id;
        public string displayName;
    }

    [CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestAreaCatalog, fileName = "AreaCatalog_")]
    public class AreaCatalogSO : ScriptableObject
    {
        [SerializeField] private List<AreaCatalogEntry> entries = new();

        public IReadOnlyList<AreaCatalogEntry> Entries => entries;

        public bool TryGetById(string areaId, out AreaCatalogEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(areaId))
                return false;

            foreach (AreaCatalogEntry e in entries)
            {
                if (e != null && e.id == areaId)
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
            foreach (AreaCatalogEntry e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.id))
                    continue;

                if (!seen.Add(e.id))
                    Debug.LogWarning($"[AreaCatalogSO] Duplicate area id '{e.id}' in '{name}'.", this);
            }
        }
#endif
    }
}
