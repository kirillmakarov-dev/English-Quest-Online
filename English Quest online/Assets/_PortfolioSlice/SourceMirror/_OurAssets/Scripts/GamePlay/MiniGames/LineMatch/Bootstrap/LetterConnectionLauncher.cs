using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterConnectionLauncher : MonoBehaviour
    {
        [SerializeField] private LetterConnectionBootstrap bootstrap;
        [SerializeField] private LetterConnectionLevelConfigSO levelConfig;

        private void Awake()
        {
            if (bootstrap == null)
            {
                bootstrap = FindFirstObjectByType<LetterConnectionBootstrap>();
            }
        }

        public void LaunchMiniGame(PlayerInteraction interactor = null)
        {
            if (bootstrap == null)
            {
                return;
            }

            bootstrap.SetLevelConfig(levelConfig, interactor);
        }
    }
}
