using System;
using UnityEngine;

namespace Puzzle.Gameplay.Features.WordReveal
{
    [Serializable]
    public struct WordTranslationEntry
    {
        [Tooltip("The English word (case-insensitive lookup key).")]
        public string word;

        [Tooltip("The translated text shown in the popup (Arabic, Hebrew, etc.).")]
        public string translation;
    }
}
