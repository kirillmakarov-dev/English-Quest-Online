using EnglishKingdom.LevelSystem;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Authoring + "/Player Level Provider")]
public class PlayerLevelProvider : MonoBehaviour, IPlayerLevelProvider
{
    public int CurrentLevel
    {
        get
        {
            if (ServiceLocator.For(this).TryGet(out ILevelService levelService))
                return Mathf.Max(1, levelService.Level);

            return 1;
        }
    }

    private ILevelService _levelService;

    private void Awake()
    {
        ServiceLocator.For(this).Register<IPlayerLevelProvider>(this);
    }

    private void OnEnable()
    {
        TrySubscribeToLevelService();
    }

    private void Start()
    {
        TrySubscribeToLevelService();
    }

    private void OnDisable()
    {
        UnsubscribeFromLevelService();
    }

    private void OnDestroy()
    {
        UnsubscribeFromLevelService();
        ServiceLocator.DeregisterFor<IPlayerLevelProvider>(this);
    }

    private void TrySubscribeToLevelService()
    {
        if (_levelService != null)
            return;

        if (!ServiceLocator.For(this).TryGet(out ILevelService levelService))
            return;

        _levelService = levelService;
        _levelService.OnLevelUp += HandleLevelChanged;
        _levelService.OnReady += HandleLevelChanged;

        if (_levelService.IsReady)
            HandleLevelChanged();
    }

    private void UnsubscribeFromLevelService()
    {
        if (_levelService == null)
            return;

        _levelService.OnLevelUp -= HandleLevelChanged;
        _levelService.OnReady -= HandleLevelChanged;
        _levelService = null;
    }

    private void HandleLevelChanged(int _) => ReevaluateQuestRequirements();

    private void HandleLevelChanged() => ReevaluateQuestRequirements();

    private void ReevaluateQuestRequirements()
    {
        if (ServiceLocator.For(this).TryGet(out IQuestService questService))
            questService.ReevaluateQuestRequirements();
    }
}
