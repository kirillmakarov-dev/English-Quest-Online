using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Hides gameplay HUD while dialogue is active. Listens to <see cref="IDialogueService"/> so it
/// works with both cinematic and legacy dialogue managers.
/// </summary>
public class DialogueHudHider : MonoBehaviour
{
    [SerializeField] private Transform _canvasRoot;
    [SerializeField] private GameObject[] _additionalRootsToHide;
    [SerializeField] private bool _autoFindHudCanvas = true;
    [SerializeField] private bool _hideQuestObjectiveUi = true;
    [SerializeField] private bool _hideNpcNameTags = true;

    private readonly Dictionary<GameObject, bool> _savedStates = new();
    private readonly List<GameObject> _hiddenTargets = new();
    private int _hideDepth;

    private IDialogueService _dialogue;
    private bool _subscribed;

    private void Awake()
    {
        if (_canvasRoot == null)
            _canvasRoot = transform;
    }

    private void OnEnable() => Subscribe();

    private void OnDisable()
    {
        Unsubscribe();

        if (_hideDepth > 0)
            RestoreHiddenTargets();
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        ServiceLocator locator = ServiceLocator.For(this);
        if (locator == null || !locator.TryGet(out _dialogue) || !UnityLifetime.IsAlive(_dialogue))
            return;

        _dialogue.OnDialogueStart += HandleDialogueStart;
        _dialogue.OnDialogueEnd += HandleDialogueEnd;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (UnityLifetime.IsAlive(_dialogue))
        {
            _dialogue.OnDialogueStart -= HandleDialogueStart;
            _dialogue.OnDialogueEnd -= HandleDialogueEnd;
        }

        _dialogue = null;
        _subscribed = false;
    }

    private void HandleDialogueStart()
    {
        if (++_hideDepth != 1)
            return;

        HideGameplayHud();
    }

    private void HandleDialogueEnd()
    {
        if (_hideDepth == 0)
            return;

        if (--_hideDepth != 0)
            return;

        RestoreHiddenTargets();
    }

    private void HideGameplayHud()
    {
        _hiddenTargets.Clear();

        if (_autoFindHudCanvas)
        {
            var hudCanvas = GameObject.Find("HUDCanvas");
            if (hudCanvas != null)
                HideObject(hudCanvas);
        }

        if (_canvasRoot != null)
        {
            for (int i = 0; i < _canvasRoot.childCount; i++)
            {
                var child = _canvasRoot.GetChild(i);
                if (IsDialoguePanel(child))
                    continue;

                HideObject(child.gameObject);
            }
        }

        if (_additionalRootsToHide != null)
        {
            foreach (var root in _additionalRootsToHide)
            {
                if (root != null)
                    HideObject(root);
            }
        }

        if (_hideQuestObjectiveUi)
            HideQuestObjectiveUi();

        if (_hideNpcNameTags)
            HideNpcNameTags();
    }

    private static bool IsDialoguePanel(Transform child)
    {
        return child.GetComponentInChildren<CinematicDialogueManager>(true) != null
               || child.GetComponentInChildren<DialogueManager>(true) != null;
    }

    private void HideQuestObjectiveUi()
    {
        foreach (var questUi in FindObjectsByType<QuestTargetUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            HideObject(questUi.gameObject);

        foreach (var worldDisplay in FindObjectsByType<QuestTargetWorldDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (worldDisplay.gameObject != null)
                HideObject(worldDisplay.gameObject);
        }
    }

    private void HideNpcNameTags()
    {
        foreach (var nameTag in FindObjectsByType<UI_NameTagView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            HideObject(nameTag.gameObject);
    }

    private void HideObject(GameObject target)
    {
        if (target == null || _hiddenTargets.Contains(target))
            return;

        if (!_savedStates.ContainsKey(target))
            _savedStates[target] = target.activeSelf;

        target.SetActive(false);
        _hiddenTargets.Add(target);
    }

    private void RestoreHiddenTargets()
    {
        for (int i = _hiddenTargets.Count - 1; i >= 0; i--)
        {
            var target = _hiddenTargets[i];
            if (target == null)
                continue;

            if (_savedStates.TryGetValue(target, out bool wasActive))
                target.SetActive(wasActive);
        }

        _hiddenTargets.Clear();
        _savedStates.Clear();
    }
}
