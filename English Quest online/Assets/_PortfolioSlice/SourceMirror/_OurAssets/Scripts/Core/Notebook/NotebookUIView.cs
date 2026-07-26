using System.Collections.Generic;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotebookUIView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _notebookPanel;
    [SerializeField] private GameObject _topicSelectionPanel;
    [SerializeField] private GameObject _pageViewerPanel;

    [Header("Topic Selection")]
    [SerializeField] private Transform _topicListContainer;
    [SerializeField] private GameObject _topicButtonPrefab;

    [Header("Page Viewer")]
    [SerializeField] private Image _pageDisplay;
    [SerializeField] private TextMeshProUGUI _topicTitleText;
    [SerializeField] private TextMeshProUGUI _pageCounterText;
    [SerializeField] private Button _nextPageButton;
    [SerializeField] private Button _previousPageButton;

    [Header("Controls")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _backButton;

    [Header("Feedbacks")]
    [SerializeField] private MMF_Player _openFeedback;
    [SerializeField] private MMF_Player _closeFeedback;
    [SerializeField] private MMF_Player _pageFlipFeedback;

    public System.Action<NotebookTopic> OnTopicSelected;
    public System.Action OnClosePressed;
    public System.Action OnBackPressed;
    public System.Action OnNextPage;
    public System.Action OnPreviousPage;

    private void Awake()
    {
        _closeButton?.onClick.AddListener(() => OnClosePressed?.Invoke());
        _backButton?.onClick.AddListener(() => OnBackPressed?.Invoke());
        _nextPageButton?.onClick.AddListener(() => OnNextPage?.Invoke());
        _previousPageButton?.onClick.AddListener(() => OnPreviousPage?.Invoke());
    }

    public void Open()
    {
        _notebookPanel.SetActive(true);
        _openFeedback?.PlayFeedbacks();
    }

    public void Hide()
    {
        _closeFeedback?.PlayFeedbacks();
        _notebookPanel.SetActive(false);
    }

    public void ShowTopicSelection(List<NotebookTopic> topics)
    {
        _topicSelectionPanel.SetActive(true);
        _pageViewerPanel.SetActive(false);

        foreach (Transform child in _topicListContainer)
            Destroy(child.gameObject);

        foreach (var topic in topics)
        {
            var go = Instantiate(_topicButtonPrefab, _topicListContainer);

            var image = go.GetComponentInChildren<Image>();
            if (topic.thumnail != null)
                image.sprite = topic.thumnail;

            var capturedTopic = topic;
            var btn = go.GetComponent<Button>();
            btn?.onClick.AddListener(() => OnTopicSelected?.Invoke(capturedTopic));
        }
    }

    public void ShowPageViewer(NotebookTopic topic, Sprite page, int pageNumber, int totalPages)
    {
        _topicSelectionPanel.SetActive(false);
        _pageViewerPanel.SetActive(true);

        if (_topicTitleText != null) _topicTitleText.text = topic.topicName;
        UpdatePage(page, pageNumber, totalPages);
    }

    public void UpdatePage(Sprite page, int pageNumber, int totalPages)
    {
        if (_pageDisplay != null) _pageDisplay.sprite = page;
        if (_pageCounterText != null) _pageCounterText.text = $"{pageNumber} / {totalPages}";
        _pageFlipFeedback?.PlayFeedbacks();
    }
}
