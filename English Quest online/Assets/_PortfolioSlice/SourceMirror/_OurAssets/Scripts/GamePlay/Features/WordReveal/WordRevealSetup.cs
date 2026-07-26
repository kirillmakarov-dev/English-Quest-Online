using TMPro;
using UnityEngine;

namespace Puzzle.Gameplay.Features.WordReveal
{
    /// <summary>
    /// Bootstrap — drop this component anywhere in a scene to make a
    /// <see cref="TMP_Text"/> sentence interactable (Duolingo-style word reveal).
    ///
    /// How to use:
    ///   1. Add this component to any GameObject in a Canvas.
    ///   2. Assign <see cref="_targetText"/>  — the sentence TextMeshProUGUI.
    ///   3. Assign <see cref="_popupView"/>   — the WordRevealPopupView prefab instance on the same Canvas.
    ///   4. Assign <see cref="_database"/>    — a WordTranslationDatabaseSO asset.
    ///   5. Enable "Raycast Target" on the TMP_Text component.
    ///   Done. No other setup needed.
    /// </summary>
    public class WordRevealSetup : MonoBehaviour
    {
        [Header("Required")]
        [Tooltip("The TMP_Text component whose words should be tappable.")]
        [SerializeField] private TMP_Text _targetText;

        [Tooltip("The popup view prefab instance on the same Canvas.")]
        [SerializeField] private WordRevealPopupView _popupView;

        [Tooltip("ScriptableObject containing all English → translation pairs.")]
        [SerializeField] private WordTranslationDatabaseSO _database;

        private WordRevealPresenter _presenter;

        private void Awake()
        {
            if (!ValidateReferences())
                return;

            var detector = _targetText.gameObject.GetComponent<TMPWordClickDetector>()
                        ?? _targetText.gameObject.AddComponent<TMPWordClickDetector>();

            var provider = new SOWordTranslationProvider(_database);

            _presenter = new WordRevealPresenter(detector, provider, _popupView);
            _presenter.Initialize();
        }

        /// <summary>
        /// Swaps the translation database at runtime. Safe to call multiple times (e.g., when
        /// the parent word-game step changes its prompt). Pass null to disable word reveal.
        /// </summary>
        public void SetDatabase(WordTranslationDatabaseSO database)
        {
            _database = database;

            _presenter?.Dispose();
            _presenter = null;

            if (database == null) return;
            if (_targetText == null || _popupView == null) return;

            var detector = _targetText.gameObject.GetComponent<TMPWordClickDetector>()
                        ?? _targetText.gameObject.AddComponent<TMPWordClickDetector>();

            var provider = new SOWordTranslationProvider(database);
            _presenter = new WordRevealPresenter(detector, provider, _popupView);
            _presenter.Initialize();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        private bool ValidateReferences()
        {
            if (_targetText == null)
            {
                AppLog.Error($"[WordRevealSetup] '{nameof(_targetText)}' is not assigned on '{name}'.", this);
                return false;
            }

            if (_popupView == null)
            {
                AppLog.Error($"[WordRevealSetup] '{nameof(_popupView)}' is not assigned on '{name}'.", this);
                return false;
            }

            if (_database == null)
            {
                AppLog.Error($"[WordRevealSetup] '{nameof(_database)}' is not assigned on '{name}'.", this);
                return false;
            }

            return true;
        }
    }
}
