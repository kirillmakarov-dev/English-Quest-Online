using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TextTyper : MonoBehaviour
{
    private Coroutine typingCoroutine;
    private string _currentFullText;
    private TextMeshProUGUI _currentComponent;
    private Action _currentOnComplete;

    public bool IsTyping { get; private set; }

    public void TypeText(string text, TextMeshProUGUI textComponent, float speed, Action onComplete = null)
    {
        StopTyping();

        if (textComponent == null)
        {
            onComplete?.Invoke();
            return;
        }

        _currentFullText = text;
        _currentComponent = textComponent;
        _currentOnComplete = onComplete;
        typingCoroutine = StartCoroutine(TypeCoroutine(text, textComponent, speed, onComplete));
    }

    public void CompleteInstantly()
    {
        if (!IsTyping || _currentComponent == null)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        _currentComponent.text = _currentFullText;
        IsTyping = false;

        var callback = _currentOnComplete;
        ClearTypingState();
        callback?.Invoke();
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        IsTyping = false;
        ClearTypingState();
    }

    private void ClearTypingState()
    {
        _currentFullText = null;
        _currentComponent = null;
        _currentOnComplete = null;
    }

    private IEnumerator TypeCoroutine(string text, TextMeshProUGUI textComponent, float speed, Action onComplete)
    {
        IsTyping = true;
        textComponent.text = "";

        foreach (char letter in text.ToCharArray())
        {
            textComponent.text += letter;
            yield return new WaitForSeconds(speed);
        }

        IsTyping = false;
        ClearTypingState();
        onComplete?.Invoke();
    }
}
