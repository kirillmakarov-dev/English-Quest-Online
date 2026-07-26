using System;
using UnityEngine;

namespace Puzzle.Gameplay.Features.WordReveal
{
    /// <summary>
    /// Pure-C# presenter — no Unity component dependencies.
    /// Connects <see cref="TMPWordClickDetector"/> → <see cref="IWordTranslationProvider"/> → <see cref="IWordRevealPopup"/>.
    ///
    /// Lifecycle:
    ///   1. Construct with dependencies.
    ///   2. Call <see cref="Initialize"/> once.
    ///   3. Call <see cref="Dispose"/> when done (e.g. OnDestroy on the owner MonoBehaviour).
    /// </summary>
    public sealed class WordRevealPresenter : IDisposable
    {
        private readonly TMPWordClickDetector _detector;
        private readonly IWordTranslationProvider _provider;
        private readonly IWordRevealPopup _popup;

        /// <summary>Maximum number of consecutive words to try as a phrase.</summary>
        private const int MaxPhraseWords = 3;

        private string _lastRevealedWord;
        private bool _disposed;

        public WordRevealPresenter(
            TMPWordClickDetector detector,
            IWordTranslationProvider provider,
            IWordRevealPopup popup)
        {
            _detector = detector;
            _provider = provider;
            _popup = popup;
        }

        /// <summary>Subscribe to click events. Call once after construction.</summary>
        public void Initialize()
        {
            _detector.WordClicked += OnWordClicked;
        }

        private void OnWordClicked(string[] allWords, int clickedIndex, Vector2 screenPosition)
        {
            // Try the longest matching phrase first, shrinking down to a single word.
            string matchedPhrase = null;
            string translation = null;
            int matchedStart = clickedIndex;
            int matchedLength = 1;

            for (int length = MaxPhraseWords; length >= 1 && matchedPhrase == null; length--)
            {
                // All windows of this length that contain the clicked word.
                int startMin = Mathf.Max(0, clickedIndex - length + 1);
                int startMax = Mathf.Min(allWords.Length - length, clickedIndex);

                for (int start = startMin; start <= startMax; start++)
                {
                    string candidate = string.Join(" ", allWords, start, length);
                    if (_provider.TryGetTranslation(candidate, out string t))
                    {
                        matchedPhrase = candidate;
                        translation = t;
                        matchedStart = start;
                        matchedLength = length;
                        break;
                    }
                }
            }

            if (matchedPhrase == null)
            {
                // Nothing matched — dismiss any open popup silently.
                _popup.Hide();
                _lastRevealedWord = null;
                return;
            }

            // Toggle off when the same word/phrase is tapped again.
            if (_popup.IsVisible && string.Equals(matchedPhrase, _lastRevealedWord, StringComparison.OrdinalIgnoreCase))
            {
                _popup.Hide();
                _lastRevealedWord = null;
                return;
            }

            _lastRevealedWord = matchedPhrase;
            Vector2 phraseScreenPosition = _detector.GetScreenPositionForWordRange(matchedStart, matchedLength);
            _popup.Show(translation, phraseScreenPosition);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _detector.WordClicked -= OnWordClicked;
        }
    }
}
