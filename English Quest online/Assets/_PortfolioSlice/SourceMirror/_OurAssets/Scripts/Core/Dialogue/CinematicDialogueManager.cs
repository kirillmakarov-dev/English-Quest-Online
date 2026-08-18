using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityServiceLocator;

/// <summary>
/// Kingdom Come-style cinematic dialogue: NPC camera, bottom subtitles, Enter/click to advance.
/// Uses the same DialogueNode ScriptableObjects as DialogueManager.
/// Guide/dev skip: hold Left Shift + Right Shift together for one second.
/// </summary>
public class CinematicDialogueManager : StaticInstance<CinematicDialogueManager>, IDialogueService
{
    const float GuideSkipHoldDuration = 1f;
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button clickCatcher;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.02f;

    public event Action<DialogueActionType, string> OnDialogueAction;
    public event Action OnDialogueStart;
    public event Action OnDialogueEnd;

    private TextTyper _textTyper;
    private Transform _speaker;
    private Transform _localPlayer;
    private DialogueNode _currentNode;
    private bool _isDialogueActive;
    private bool _lineReady;
    private bool _waitingForAdvance;
    private float _guideSkipHoldTimer;

    public bool IsDialogueActive => _isDialogueActive;

    private struct DeferredAction
    {
        public DialogueActionType Type;
        public string Parameter;
    }

    private readonly List<DeferredAction> _deferredActions = new();

    protected override void Awake()
    {
        base.Awake();

        if (ServiceLocator.For(this).TryGet<IDialogueService>(out _))
        {
            AppLog.Warning("[CinematicDialogueManager] Another IDialogueService is already registered in this scene. Disabling duplicate.");
            enabled = false;
            return;
        }

        ServiceLocator.For(this).Register<IDialogueService>(this);

        EnsureUi();
        _textTyper = GetComponent<TextTyper>() ?? gameObject.AddComponent<TextTyper>();

        if (clickCatcher != null)
            clickCatcher.onClick.AddListener(HandleAdvanceInput);

        if (dialoguePanel != null && !_isDialogueActive)
            dialoguePanel.SetActive(false);
    }

    protected override void OnDestroy()
    {
        if (clickCatcher != null)
            clickCatcher.onClick.RemoveListener(HandleAdvanceInput);

        ServiceLocator.DeregisterFor<IDialogueService>(this);
        base.OnDestroy();
    }

    void Update()
    {
        if (!_isDialogueActive)
            return;

        if (TryGuideSkipInput())
            return;

        if (!_waitingForAdvance)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            HandleAdvanceInput();
    }

    bool TryGuideSkipInput()
    {
        Keyboard keyboard = Keyboard.current;
        bool bothShiftsHeld = keyboard != null &&
                              keyboard.leftShiftKey.isPressed &&
                              keyboard.rightShiftKey.isPressed;
        if (!bothShiftsHeld)
        {
            _guideSkipHoldTimer = 0f;
            return false;
        }

        _guideSkipHoldTimer += Time.unscaledDeltaTime;
        if (_guideSkipHoldTimer < GuideSkipHoldDuration)
            return false;

        _guideSkipHoldTimer = 0f;
        SkipDialogue();
        return true;
    }

    void SkipDialogue()
    {
        if (!_isDialogueActive)
            return;

        EndDialogue();
    }

    public void StartDialogue(
        DialogueNode startNode,
        Transform speaker = null,
        Transform localPlayer = null,
        Sprite speakerSprite = null,
        string speakerName = "")
    {
        _deferredActions.Clear();
        _speaker = speaker;
        _localPlayer = localPlayer;
        _isDialogueActive = true;
        _lineReady = false;
        _waitingForAdvance = false;
        _guideSkipHoldTimer = 0f;

        OnDialogueStart?.Invoke();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        DisplayNode(startNode);
    }

    void DisplayNode(DialogueNode node)
    {
        _currentNode = node;

        if (node == null)
        {
            EndDialogue();
            return;
        }

        PlayVoiceForNode(node);

        if (node.actionType != DialogueActionType.None)
            HandleDialogueAction(node.actionType, node.actionParameter);

        _lineReady = false;
        _waitingForAdvance = false;

        if (_textTyper != null)
        {
            _textTyper.TypeText(node.dialogueText, subtitleText, typingSpeed, OnLinePresentationComplete);
        }
        else
        {
            if (subtitleText != null)
                subtitleText.text = node.dialogueText;
            OnLinePresentationComplete();
        }
    }

    void OnLinePresentationComplete()
    {
        StartCoroutine(WaitForLineReadyRoutine());
    }

    IEnumerator WaitForLineReadyRoutine()
    {
        while (!IsLineReady())
            yield return null;

        _lineReady = true;
        _waitingForAdvance = true;
    }

    bool IsLineReady()
    {
        if (_textTyper != null && _textTyper.IsTyping)
            return false;

        return true;
    }

    void HandleAdvanceInput()
    {
        if (!_isDialogueActive || _currentNode == null)
            return;

        if (_textTyper != null && _textTyper.IsTyping)
        {
            _textTyper.CompleteInstantly();
            return;
        }

        if (!_lineReady)
            return;

        AdvanceToNextNode();
    }

    void AdvanceToNextNode()
    {
        _waitingForAdvance = false;
        _lineReady = false;

        var node = _currentNode;
        if (node.choices == null || node.choices.Count == 0)
        {
            EndDialogue();
            return;
        }

        if (node.choices.Count == 1)
        {
            DisplayNode(node.choices[0].nextNode);
            return;
        }

        // Future branching: default to first choice for now.
        DisplayNode(node.choices[0].nextNode);
    }

    void PlayVoiceForNode(DialogueNode node)
    {
        if (node.voiceClip == null)
            return;
        if (ServiceLocator.For(this).TryGet<IAudioService>(out var audioService))
            audioService.PlayVoice(node.voiceClip);
    }

    public void EndDialogue()
    {
        ProcessDeferredActions();

        _isDialogueActive = false;
        _waitingForAdvance = false;
        _lineReady = false;
        _currentNode = null;

        OnDialogueEnd?.Invoke();

        if (_textTyper != null)
            _textTyper.StopTyping();

        if (ServiceLocator.For(this).TryGet<IAudioService>(out var audioService))
            audioService.StopVoice();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        _speaker = null;
        _localPlayer = null;
        _guideSkipHoldTimer = 0f;
    }

    void HandleDialogueAction(DialogueActionType type, string parameter)
    {
        if (ShouldDeferAction(type))
        {
            _deferredActions.Add(new DeferredAction { Type = type, Parameter = parameter });
            return;
        }

        ExecuteAction(type, parameter);
    }

    static bool ShouldDeferAction(DialogueActionType type) => type == DialogueActionType.GiveReward;

    void ProcessDeferredActions()
    {
        foreach (var action in _deferredActions)
            OnDialogueAction?.Invoke(action.Type, action.Parameter);
        _deferredActions.Clear();
    }

    void ExecuteAction(DialogueActionType type, string parameter)
    {
        OnDialogueAction?.Invoke(type, parameter);

        switch (type)
        {
            case DialogueActionType.OpenShop:
            case DialogueActionType.CloseDialogue:
            case DialogueActionType.TutorialFinish:
                EndDialogue();
                break;
        }
    }

    void EnsureUi()
    {
        if (dialoguePanel != null && subtitleText != null && clickCatcher != null)
            return;

        var rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = gameObject.AddComponent<RectTransform>();

        if (dialoguePanel == null)
            dialoguePanel = gameObject;

        StretchFullScreen(rootRect);

        if (clickCatcher == null)
        {
            var catcherObject = new GameObject("ClickCatcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            catcherObject.transform.SetParent(transform, false);
            var catcherRect = catcherObject.GetComponent<RectTransform>();
            StretchFullScreen(catcherRect);

            var image = catcherObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            clickCatcher = catcherObject.GetComponent<Button>();
            clickCatcher.transition = Selectable.Transition.None;
        }

        if (subtitleText == null)
        {
            var barObject = new GameObject("SubtitleBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barObject.transform.SetParent(transform, false);

            var barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.1f, 0.04f);
            barRect.anchorMax = new Vector2(0.9f, 0.16f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;

            var barImage = barObject.GetComponent<Image>();
            barImage.color = new Color(0f, 0f, 0f, 0.72f);
            barImage.raycastTarget = false;

            var textObject = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(barObject.transform, false);

            var textRect = textObject.GetComponent<RectTransform>();
            StretchFullScreen(textRect);
            textRect.offsetMin = new Vector2(24f, 12f);
            textRect.offsetMax = new Vector2(-24f, -12f);

            subtitleText = textObject.GetComponent<TextMeshProUGUI>();
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontSize = 36f;
            subtitleText.color = Color.white;
            subtitleText.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    static void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
    }
}
