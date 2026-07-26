using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldMapNpcInteractable : MonoBehaviour, IInteractable
{
    private const string DefaultMapAssetPath = "Assets/_OurAssets/Data/WorldMap/OpenWorldMap.asset";

    [Header("World Map")]
    [SerializeField] private WorldMapDefinitionSO _map;
    [SerializeField] private string _mapAssetPath = DefaultMapAssetPath;
    [SerializeField] private string _currentNodeId;

    [Header("Interaction Settings")]
    [SerializeField] private string _interactionPrompt = "Travel";

    public string InteractionPrompt => _interactionPrompt;

    public bool CanInteract => enabled && ResolveMap() != null && !string.IsNullOrEmpty(_currentNodeId);

    public bool Interact(PlayerInteraction interactor)
    {
        if (interactor == null)
            return false;

        WorldMapUI mapUI = FindWorldMapUi();
        if (mapUI == null)
        {
            AppLog.Error("[WorldMapNpcInteractable] WorldMapUI not found.");
            return false;
        }

        if (mapUI.IsOpen)
        {
            mapUI.Close();
            return true;
        }

        if (!CanInteract)
            return false;

        interactor.LockInteraction(this);
        mapUI.Open(ResolveMap(), _currentNodeId, interactor);

        if (!mapUI.IsOpen)
        {
            interactor.UnlockInteraction(this);
            return false;
        }

        return true;
    }

    private static WorldMapUI FindWorldMapUi()
    {
        return FindFirstObjectByType<WorldMapUI>(FindObjectsInactive.Include);
    }

    private WorldMapDefinitionSO ResolveMap()
    {
        if (_map != null)
            return _map;

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(_mapAssetPath))
            _map = AssetDatabase.LoadAssetAtPath<WorldMapDefinitionSO>(_mapAssetPath);
#endif

        return _map;
    }
}
