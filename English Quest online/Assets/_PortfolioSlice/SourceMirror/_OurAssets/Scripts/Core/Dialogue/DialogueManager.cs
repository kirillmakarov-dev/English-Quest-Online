using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

public class DialogueManager : StaticInstance<DialogueManager>, IDialogueService
{
    private const string DialogueEndNextText = "סיים";

    private static readonly PlayerLockSystem.LockType[] DialogueLocks =
    {
        PlayerLockSystem.LockType.Movement,
        PlayerLockSystem.LockType.Camera,
        PlayerLockSystem.LockType.Interaction,
        PlayerLockSystem.LockType.Cursor,
        PlayerLockSystem.LockType.GameplayInput
    };

    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;

    [Header("Settings")]
    public float typingSpeed = 0.02f;
    private const string MissingDialogueText = "Ready for the next lesson.";

    public event Action<DialogueActionType, string> OnDialogueAction;
    public event Action OnDialogueStart;
    public event Action OnDialogueEnd;

    private TextTyper textTyper;
    private Sprite currentPortrait;
    private bool isDialogueActive = false;
    public bool IsDialogueActive => isDialogueActive;
    private IPlayerLockSystem inputLocker;
    private bool ownsInputLock;
    private bool usingCursorFallback;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    
    // Struct to store deferred actions cleanly
    private struct DeferredAction
    {
        public DialogueActionType Type;
        public string Parameter;
    }
    private List<DeferredAction> _deferredActions = new List<DeferredAction>();

    protected override void Awake()
    {
        base.Awake();

        if (ServiceLocator.For(this).TryGet<IDialogueService>(out _))
        {
            AppLog.Warning("[DialogueManager] Another IDialogueService is already registered in this scene. Disabling duplicate.");
            enabled = false;
            return;
        }

        ServiceLocator.For(this).Register<IDialogueService>(this);

        textTyper = GetComponent<TextTyper>();
        if (textTyper == null)
            textTyper = gameObject.AddComponent<TextTyper>();

        // Only hide if we aren't already active (e.g. opened via script before Awake ran)
        if (dialoguePanel != null && !isDialogueActive)
            dialoguePanel.SetActive(false);
    }

    protected override void OnDestroy()
    {
        ReleaseDialogueInput();
        ServiceLocator.DeregisterFor<IDialogueService>(this);
        base.OnDestroy();
    }

    public void StartDialogue(
        DialogueNode startNode,
        Transform speaker = null,
        Transform localPlayer = null,
        Sprite speakerSprite = null,
        string speakerName = "")
    {
        textTyper?.StopTyping();
        ClearChoices();
        ReleaseDialogueInput();
        AcquireDialogueInput(localPlayer);
        _deferredActions.Clear();
        isDialogueActive = true;
        OnDialogueStart?.Invoke();
        currentPortrait = speakerSprite;
        if (nameText != null) nameText.text = speakerName;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
            
        DisplayNode(startNode);
    }
    public void DisplayNode(DialogueNode node)
    {
        if (node == null)
        {
            EndDialogue();
            return;
        }

        // Play voice audio if available
        if (node.voiceClip != null && ServiceLocator.For(this).TryGet<IAudioService>(out var audioService))
        {
            audioService.PlayVoice(node.voiceClip);
        }

        // 1. Update UI Elements
        if (nameText != null) nameText.text = node.speakerName;
        if (portraitImage != null && currentPortrait != null)
        {
            portraitImage.sprite = currentPortrait;
            portraitImage.gameObject.SetActive(true);
        }
        else if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(false);
        }

        // 2. Perform Action (if any occurs *on enter* of the node, or maybe on exit. 
        // Usually, actions might happen when you reach the node. 
        // Based on "Option 2", the node carries the action.)
        if (node.actionType != DialogueActionType.None)
        {
            HandleDialogueAction(node.actionType, node.actionParameter);
        }

        // 3. Type text
        ClearChoices();
        string resolvedText = ResolveDialogueText(node);

        if (textTyper != null)
        {
            textTyper.TypeText(resolvedText, dialogueText, typingSpeed, () => ShowChoices(node));
        }
        else
        {
            // Fallback if typer missing
            if (dialogueText != null) dialogueText.text = resolvedText;
            ShowChoices(node);
        }
    }

    private static string ResolveDialogueText(DialogueNode node)
    {
        if (node != null && !string.IsNullOrWhiteSpace(node.dialogueText))
            return node.dialogueText;

        return MissingDialogueText;
    }

    void ClearChoices()
    {
        if (choiceContainer == null) return;
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void ShowChoices(DialogueNode node)
    {
        if (choiceContainer == null || choiceButtonPrefab == null) return;

        // If no choices, create a "Continue" or "End" button automatically, 
        // or if it was just an info node, maybe wait for click.
        // For MapleStory style, usually there's a "Next" button.
        
        if (node.choices == null || node.choices.Count == 0)
        {
            CreateChoiceButton(DialogueEndNextText, null); // Example default
        }
        else
        {
            foreach (var choice in node.choices)
            {
                CreateChoiceButton(choice.choiceText, choice.nextNode);
            }
        }
    }

    void CreateChoiceButton(string text, DialogueNode nextNode)
    {
        GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
        buttonObj.SetActive(true);

        TextMeshProUGUI btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.text = text;

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnChoiceSelected(nextNode));
        }
    }

    void OnChoiceSelected(DialogueNode nextNode)
    {
        if (nextNode != null)
        {
            DisplayNode(nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        ProcessDeferredActions();

        isDialogueActive = false;
        OnDialogueEnd?.Invoke();
        
        if (textTyper != null)
            textTyper.StopTyping();

        if (ServiceLocator.For(this).TryGet<IAudioService>(out var audioService))
        {
            audioService.StopVoice();
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        // Clean up choices
        ClearChoices();
        ReleaseDialogueInput();
    }

    private void AcquireDialogueInput(Transform localPlayer)
    {
        if (TryResolveInputLocker(localPlayer, out inputLocker))
        {
            inputLocker.Lock(this, DialogueLocks);
            ownsInputLock = true;
            usingCursorFallback = false;
            return;
        }

        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        usingCursorFallback = true;
    }

    private bool TryResolveInputLocker(Transform localPlayer, out IPlayerLockSystem locker)
    {
        if (localPlayer != null)
        {
            locker = localPlayer.GetComponentInParent<PlayerLockSystem>();
            if (locker != null)
                return true;
        }

        if (ServiceLocator.For(this).TryGet(out locker))
            return true;

        locker = null;
        return false;
    }

    private void ReleaseDialogueInput()
    {
        if (ownsInputLock && inputLocker != null)
            inputLocker.Unlock(this, DialogueLocks);

        if (usingCursorFallback)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }

        inputLocker = null;
        ownsInputLock = false;
        usingCursorFallback = false;
    }

    private void HandleDialogueAction(DialogueActionType type, string parameter)
    {
        AppLog.Info($"Handling Action: {type} with param: {parameter}");

        if (ShouldDeferAction(type))
        {
            _deferredActions.Add(new DeferredAction { Type = type, Parameter = parameter });
            return;
        }

        ExecuteAction(type, parameter);
    }

    private bool ShouldDeferAction(DialogueActionType type)
    {
        // Add any other action types here that should wait until the dialogue closes
        return type == DialogueActionType.GiveReward;
    }

    private void ProcessDeferredActions()
    {
        foreach (var action in _deferredActions)
        {
            // We invoke the event directly to avoid re-triggering logic
            OnDialogueAction?.Invoke(action.Type, action.Parameter);
        }
        _deferredActions.Clear();
    }

    private void ExecuteAction(DialogueActionType type, string parameter)
    {
        OnDialogueAction?.Invoke(type, parameter);

        switch (type)
        {
            case DialogueActionType.StartQuest:
                break;
            case DialogueActionType.CompleteQuest:
                break;
            case DialogueActionType.OpenShop:
                 EndDialogue(); 
                break;
            case DialogueActionType.CloseDialogue:
                EndDialogue();
                break;
            case DialogueActionType.TutorialFinish: 
                // Dirty Implementation for tutorial completion
                EndDialogue();
                break;
        }
    }
}
