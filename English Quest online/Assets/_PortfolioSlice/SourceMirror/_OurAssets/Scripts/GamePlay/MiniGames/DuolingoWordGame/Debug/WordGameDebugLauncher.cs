using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Debug-only launcher. Drop this on any GameObject in the scene to test the
    /// word ordering game without a quest system or player interaction.
    ///
    /// Remove or disable before shipping.
    /// </summary>
    public class WordGameDebugLauncher : MonoBehaviour
    {
        [Header("Required")]
        [Tooltip("The word-ordering bootstrap in the scene.")]
        [SerializeField] private WordGameBootstrap _bootstrap;

        [Header("Mode Source")]
        [Tooltip("GameObject that has a LetterOrderingMode or WordOrderingMode component. " +
                 "Leave empty to use this GameObject.")]
        [SerializeField] private GameObject _modeSource;

        [Header("Options")]
        [Tooltip("Open the game automatically when Play mode starts.")]
        [SerializeField] private bool _launchOnStart = true;

        [Tooltip("Press this key at runtime to (re)open the game.")]
        [SerializeField] private KeyCode _launchKey = KeyCode.T;

        // ── Unity messages ────────────────────────────────────────────────────

        private void Start()
        {
            if (_launchOnStart)
                Launch();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_launchKey))
                Launch();
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 180, 44), "Launch Word Ordering (debug)"))
                Launch();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Opens the word ordering game immediately. Also callable from the Inspector context menu.</summary>
        [ContextMenu("Launch Word Ordering")]
        public void Launch()
        {
            if (_bootstrap == null)
            {
                AppLog.Error("[WordOrderingDebugLauncher] Bootstrap reference is missing.", this);
                return;
            }

            GameObject source = _modeSource != null ? _modeSource : gameObject;
            IWordGameMode mode = source.GetComponent<IWordGameMode>();

            if (mode == null)
            {
                AppLog.Error(
                    $"[WordOrderingDebugLauncher] No IWordGameMode component found on '{source.name}'. " +
                    "Add LetterOrderingMode or WordOrderingMode.", this);
                return;
            }

            // Passing null interactor skips player-lock so the mouse stays free during testing.
            _bootstrap.Open(mode, null, OnCompleted, OnClosed);
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnCompleted()
        {
            AppLog.Info("[WordOrderingDebugLauncher] Correct! Game completed successfully.");
        }

        private void OnClosed()
        {
            AppLog.Info("[WordOrderingDebugLauncher] Panel closed without a correct answer.");
        }
    }
}
