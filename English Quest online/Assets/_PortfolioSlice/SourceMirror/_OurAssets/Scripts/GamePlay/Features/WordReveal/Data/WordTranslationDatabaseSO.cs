using System;
using System.Collections.Generic;
using UnityEngine;

namespace Puzzle.Gameplay.Features.WordReveal
{
    [CreateAssetMenu(fileName = "WordTranslationDatabase", menuName = ScriptableObjectMenuPaths.FeaturesWordReveal + "/Translation Database")]
    public class WordTranslationDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<WordTranslationEntry> _entries = new List<WordTranslationEntry>();

        private Dictionary<string, string> _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.word) && !string.IsNullOrEmpty(entry.translation))
                {
                    _lookup.TryAdd(entry.word.Trim(), entry.translation);
                }
            }
        }

        public bool TryGetTranslation(string word, out string translation)
        {
#if UNITY_EDITOR
            // Always rebuild in the Editor so live SO edits are reflected immediately.
            BuildLookup();
#else
            if (_lookup == null)
                BuildLookup();
#endif
            return _lookup.TryGetValue(word.Trim(), out translation);
        }

        private void OnValidate()
        {
            // Invalidate the cache whenever the asset is edited in the Inspector.
            _lookup = null;
        }
    }
}
