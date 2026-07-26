using System.Collections;
using Cysharp.Threading.Tasks;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityServiceLocator;

namespace EnglishKingdom.UI.HUD
{
    /// <summary>
    /// Binds a scene <see cref="HealthBarView"/> to the local player's health via
    /// <see cref="ILocalPlayerHealth"/> from ServiceLocator. Optionally syncs the
    /// player name and portrait on the PlayerBar HUD.
    /// </summary>
    public class PlayerHealthHUD : MonoBehaviour
    {
        private const string UsernameKey = "username";

        [SerializeField] private HealthBarView _healthBarView;
        [SerializeField] private int _waitFrames = 120;

        [Header("Player Bar Profile")]
        [SerializeField] private Text _nameText;
        [SerializeField] private RawImage _portraitImage;
        [SerializeField] private PlayerPortraitCatalogSO _portraitCatalog;

        private Coroutine _waitCoroutine;
        private bool _bound;
        private ILocalPlayerReadiness _readiness;

        private void OnEnable()
        {
            if (_healthBarView == null)
                _healthBarView = GetComponentInChildren<HealthBarView>();

            if (ServiceLocator.For(this).TryGet(out _readiness))
                _readiness.Ready += HandleLocalPlayerReady;
            LocalPlayerHealthProvider.OnLocalHealthReady += HandleLocalHealthReady;
            ApplyProfile();

            if (TryBind())
                return;

            _waitCoroutine = StartCoroutine(WaitForHealthService());
        }

        private void OnDisable()
        {
            if (_readiness != null)
            {
                _readiness.Ready -= HandleLocalPlayerReady;
                _readiness = null;
            }
            LocalPlayerHealthProvider.OnLocalHealthReady -= HandleLocalHealthReady;

            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }

            _bound = false;
            _healthBarView?.Unbind();
        }

        private void HandleLocalPlayerReady(LocalPlayerReadyArgs args)
        {
            if (!args.Runner.ProvideInput)
                return;

            if (!RunnerOwnsHudScene(args.Runner))
                return;

            TryBind();
        }

        private bool RunnerOwnsHudScene(NetworkRunner runner)
        {
            Scene hudScene = gameObject.scene;
            if (!hudScene.IsValid())
                return false;

            Scene simulationScene = runner.SimulationUnityScene;
            if (simulationScene.IsValid() && simulationScene.handle == hudScene.handle)
                return true;

            return runner.gameObject.scene.IsValid()
                && runner.gameObject.scene.handle == hudScene.handle;
        }

        private void HandleLocalHealthReady(ILocalPlayerHealth localHealth)
        {
            if (_bound || _healthBarView == null || localHealth?.Health == null)
                return;

            _healthBarView.Bind(localHealth.Health);
            _bound = true;

            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }
        }

        private void ApplyProfile()
        {
            ApplyNameAsync().Forget();
            ApplyPortraitAsync().Forget();
        }

        private async UniTaskVoid ApplyNameAsync()
        {
            if (_nameText == null) return;

            string name = PlayerPrefs.GetString(UsernameKey, string.Empty);
            if (string.IsNullOrWhiteSpace(name))
                name = await TryLoadDisplayNameAsync();

            if (string.IsNullOrWhiteSpace(name))
                name = "Player";

            _nameText.text = name;
        }

        private static async UniTask<string> TryLoadDisplayNameAsync()
        {
            try
            {
                CoreSaveData core = await SaveManager.LoadCoreAsync();
                return core?.DisplayName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async UniTaskVoid ApplyPortraitAsync()
        {
            if (_portraitImage == null) return;

            Texture portrait = _portraitImage.texture;
            if (_portraitCatalog != null)
            {
                int avatarIndex = await TryLoadAvatarIndexAsync();
                Texture catalogPortrait = _portraitCatalog.GetPortrait(avatarIndex);
                if (catalogPortrait != null)
                    portrait = catalogPortrait;
            }

            if (portrait != null)
                _portraitImage.texture = portrait;
        }

        private static async UniTask<int> TryLoadAvatarIndexAsync()
        {
            try
            {
                CoreSaveData core = await SaveManager.LoadCoreAsync();
                return core?.SelectedAvatarIndex ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool TryBind()
        {
            if (_bound || _healthBarView == null)
                return false;

            if (!ServiceLocator.For(this).TryGet(out ILocalPlayerHealth localHealth))
                return false;

            if (localHealth?.Health == null)
                return false;

            _healthBarView.Bind(localHealth.Health);
            _bound = true;
            return true;
        }

        private IEnumerator WaitForHealthService()
        {
            for (int i = 0; i < _waitFrames; i++)
            {
                yield return null;

                if (TryBind())
                    yield break;
            }

            while (!_bound)
            {
                yield return null;
                TryBind();
            }
        }
    }
}
