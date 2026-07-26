using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class NotebookController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private NotebookContentSO _content;

    [Header("Input")]
    [SerializeField] private NotebookInputHandler _inputHandler;

    [Header("View")]
    [SerializeField] private NotebookUIView _uiView;

    private bool _isOpen = false;
    private NotebookTopic _currentTopic;
    private int _currentPageIndex;
    private List<Sprite> _currentPages;

    private void Start()
    {
        _uiView.OnTopicSelected += OnTopicSelected;
        _uiView.OnClosePressed += CloseNotebook;
        _uiView.OnBackPressed += GoBackToTopics;
        _uiView.OnNextPage += NextPage;
        _uiView.OnPreviousPage += PreviousPage;

        _uiView.Hide();
    }

    private void Update()
    {
        if (_inputHandler.TogglePressed)
        {
            ToggleNotebook();
            return;
        }

        if (!_isOpen) return;

        if (_inputHandler.ClosePressed)
        {
            CloseOrNavigateBack();
            return;
        }

        if (_currentPages == null) return;

        if (_inputHandler.NextPagePressed)
            NextPage();

        if (_inputHandler.PreviousPagePressed)
            PreviousPage();
    }

    private void ToggleNotebook()
    {
        if (_isOpen)
        {
            CloseNotebook();
            return;
        }

        if (ServiceLocator.For(this).TryGet(out IPlayerLockSystem lockSystem)
            && GameplayInputGate.IsBlockedByAnother(this, lockSystem))
            return;

        OpenNotebook();
    }

    private void OpenNotebook()
    {
        _isOpen = true;
        _currentTopic = null;
        _currentPages = null;
        _currentPageIndex = 0;

        if (ServiceLocator.For(this).TryGet(out IPlayerLockSystem lockSystem))
            lockSystem.Lock(this, PlayerLockSystem.LockType.Movement, PlayerLockSystem.LockType.Cursor, PlayerLockSystem.LockType.GameplayInput);
        UIDimmer.Instance.Show();
        _uiView.Open();
        _uiView.ShowTopicSelection(_content.notebookTopics);
    }

    private void CloseNotebook()
    {
        _isOpen = false;
        _currentTopic = null;
        _currentPages = null;

        if (ServiceLocator.For(this).TryGet(out IPlayerLockSystem lockSystem))
            lockSystem.Unlock(this, PlayerLockSystem.LockType.Movement, PlayerLockSystem.LockType.Cursor, PlayerLockSystem.LockType.GameplayInput);
        UIDimmer.Instance.Hide();
        _uiView.Hide();
    }

    private void CloseOrNavigateBack()
    {
        if (_currentPages != null)
            GoBackToTopics();
        else
            CloseNotebook();
    }

    private void GoBackToTopics()
    {
        _currentTopic = null;
        _currentPages = null;
        _currentPageIndex = 0;
        _uiView.ShowTopicSelection(_content.notebookTopics);
    }

    private void OnTopicSelected(NotebookTopic topic)
    {
        if (topic.pages == null || topic.pages.Count == 0)
        {
            AppLog.Warning($"[Notebook] Topic '{topic.topicName}' has no pages.");
            return;
        }

        _currentTopic = topic;
        _currentPages = topic.pages;
        _currentPageIndex = 0;
        _uiView.ShowPageViewer(topic, _currentPages[0], 1, _currentPages.Count);
    }

    private void NextPage()
    {
        if (_currentPages == null || _currentPageIndex >= _currentPages.Count - 1) return;
        _currentPageIndex++;
        _uiView.UpdatePage(_currentPages[_currentPageIndex], _currentPageIndex + 1, _currentPages.Count);
    }

    private void PreviousPage()
    {
        if (_currentPages == null || _currentPageIndex <= 0) return;
        _currentPageIndex--;
        _uiView.UpdatePage(_currentPages[_currentPageIndex], _currentPageIndex + 1, _currentPages.Count);
    }



    private void OnDisable()
    {
        if (_isOpen)
        {
            if (ServiceLocator.For(this).TryGet(out IPlayerLockSystem lockSystem))
                lockSystem.Unlock(this, PlayerLockSystem.LockType.Movement, PlayerLockSystem.LockType.Cursor, PlayerLockSystem.LockType.GameplayInput);
            _isOpen = false;
        }
    }
}
