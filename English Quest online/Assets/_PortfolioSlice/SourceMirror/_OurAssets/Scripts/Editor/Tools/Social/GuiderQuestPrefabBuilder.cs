#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds Guider Quest Editor prefabs and wires them into the Social panel.
/// Menu: Tools/English Kingdom/Social/Create Guider Quest Prefabs
/// </summary>
[InitializeOnLoad]
public static class GuiderQuestPrefabBuilder
{
    public const string FolderPath = "Assets/_OurAssets/Art/Prefabs/UI/Social";
    public const string ListItemPrefabPath = FolderPath + "/GuiderPlayerQuestListItem.prefab";
    public const string PanelPrefabPath = FolderPath + "/GuiderPlayerQuestPanel.prefab";
    public const string SessionPlayerListItemPath = FolderPath + "/SessionPlayerListItem.prefab";
    public const string SocialPanelCanvasPath = FolderPath + "/SocialPanelCanvas.prefab";

    static GuiderQuestPrefabBuilder()
    {
        EditorApplication.delayCall += EnsurePrefabsExistOnce;
    }

    static void EnsurePrefabsExistOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath) != null
            && AssetDatabase.LoadAssetAtPath<GameObject>(ListItemPrefabPath) != null)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(SocialPanelCanvasPath) == null)
            return;

        Debug.Log("[GuiderQuestPrefabBuilder] Missing guider quest prefabs — creating and wiring now.");
        RebuildAndWire();
    }

    [MenuItem("Tools/English Kingdom/Social/Create Guider Quest Prefabs")]
    public static void CreateFromMenu()
    {
        RebuildAndWire();
        GameObject panel = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        Selection.activeObject = panel;
        EditorGUIUtility.PingObject(panel);
        Debug.Log(
            $"[GuiderQuestPrefabBuilder] Prefabs ready:\n" +
            $"  {ListItemPrefabPath}\n" +
            $"  {PanelPrefabPath}\n" +
            $"Wired Quest button on SessionPlayerListItem and GuiderPlayerQuestPanel into SocialPanelCanvas.");
    }

    public static void RebuildAndWire()
    {
        EnsureFolder(FolderPath);

        GuiderPlayerQuestListItemView listItemPrefab = BuildListItemPrefab();
        GameObject panelPrefab = BuildPanelPrefab(listItemPrefab);
        WireSessionPlayerListItem();
        WireSocialPanelCanvas(panelPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static GuiderPlayerQuestListItemView BuildListItemPrefab()
    {
        var root = new GameObject(
            "GuiderPlayerQuestListItem",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement),
            typeof(GuiderPlayerQuestListItemView));

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(520f, 150f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.2f, 0.24f, 1f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement rootLayout = root.GetComponent<LayoutElement>();
        rootLayout.minHeight = 150f;
        rootLayout.preferredHeight = 150f;

        TextMeshProUGUI title = CreateTmp(root.transform, "Title", "Quest Name", 18f, FontStyles.Bold);
        TextMeshProUGUI state = CreateTmp(root.transform, "State", "State", 14f, FontStyles.Normal);

        var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttonRow.transform.SetParent(root.transform, false);
        buttonRow.GetComponent<LayoutElement>().preferredHeight = 30f;

        HorizontalLayoutGroup rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Button forceStart = CreateInlineButton(buttonRow.transform, "StartButton", "Start");
        Button forceComplete = CreateInlineButton(buttonRow.transform, "CompleteButton", "Complete");
        Button advance = CreateInlineButton(buttonRow.transform, "AdvanceButton", "Advance");
        Button goBack = CreateInlineButton(buttonRow.transform, "BackButton", "Back");
        Button reset = CreateInlineButton(buttonRow.transform, "ResetButton", "Reset");

        GuiderPlayerQuestListItemView view = root.GetComponent<GuiderPlayerQuestListItemView>();
        SetObjectField(view, "_titleText", title);
        SetObjectField(view, "_stateText", state);
        SetObjectField(view, "_forceStartButton", forceStart);
        SetObjectField(view, "_forceCompleteButton", forceComplete);
        SetObjectField(view, "_resetButton", reset);
        SetObjectField(view, "_advanceStepButton", advance);
        SetObjectField(view, "_goBackStepButton", goBack);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ListItemPrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<GuiderPlayerQuestListItemView>();
    }

    static GameObject BuildPanelPrefab(GuiderPlayerQuestListItemView listItemPrefab)
    {
        var root = new GameObject(
            "GuiderPlayerQuestPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(GuiderPlayerQuestPanel),
            typeof(GuiderPlayerQuestPanelView));

        RectTransform rootRt = root.GetComponent<RectTransform>();
        Stretch(rootRt, 0f);
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        var panel = new GameObject(
            "Panel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panel.transform.SetParent(root.transform, false);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(560f, 640f);
        panelRt.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

        TextMeshProUGUI header = CreateAbsoluteTmp(
            panel.transform, "HeaderText", "Quest Editor", 24f, FontStyles.Bold,
            new Vector2(20f, -20f), new Vector2(360f, 36f));

        TextMeshProUGUI status = CreateAbsoluteTmp(
            panel.transform, "StatusText", "Loading...", 16f, FontStyles.Normal,
            new Vector2(20f, -56f), new Vector2(360f, 24f));

        Button closeButton = CreateAbsoluteButton(
            panel.transform, "CloseButton", "CLOSE",
            new Vector2(-20f, -20f), new Vector2(100f, 32f), upperRight: true);
        Button refreshButton = CreateAbsoluteButton(
            panel.transform, "RefreshButton", "REFRESH",
            new Vector2(-130f, -20f), new Vector2(100f, 32f), upperRight: true);

        Transform listContainer = CreateScrollList(panel.transform);

        GuiderPlayerQuestPanelView view = root.GetComponent<GuiderPlayerQuestPanelView>();
        SetObjectField(view, "_panelRoot", panel);
        SetObjectField(view, "_headerText", header);
        SetObjectField(view, "_statusText", status);
        SetObjectField(view, "_listContainer", listContainer);
        SetObjectField(view, "_itemPrefab", listItemPrefab);
        SetObjectField(view, "_closeButton", closeButton);
        SetObjectField(view, "_refreshButton", refreshButton);

        GuiderPlayerQuestPanel controller = root.GetComponent<GuiderPlayerQuestPanel>();
        SetObjectField(controller, "_view", view);

        root.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void WireSessionPlayerListItem()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(SessionPlayerListItemPath);
        try
        {
            SessionPlayerListItemView itemView = prefabRoot.GetComponent<SessionPlayerListItemView>();
            if (itemView == null)
            {
                Debug.LogError("[GuiderQuestPrefabBuilder] SessionPlayerListItemView missing on SessionPlayerListItem.");
                return;
            }

            Transform inviteButton = prefabRoot.transform.Find("InviteButton");
            if (inviteButton == null)
            {
                Debug.LogError("[GuiderQuestPrefabBuilder] InviteButton not found on SessionPlayerListItem.");
                return;
            }

            Transform existingQuest = prefabRoot.transform.Find("QuestButton");
            GameObject questButtonGo;
            if (existingQuest != null)
            {
                questButtonGo = existingQuest.gameObject;
            }
            else
            {
                questButtonGo = Object.Instantiate(inviteButton.gameObject, prefabRoot.transform);
                questButtonGo.name = "QuestButton";
                questButtonGo.transform.SetSiblingIndex(inviteButton.GetSiblingIndex());
            }

            RectTransform questRt = questButtonGo.GetComponent<RectTransform>();
            if (questRt != null)
                questRt.sizeDelta = new Vector2(84f, questRt.sizeDelta.y);

            TextMeshProUGUI questText = questButtonGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (questText != null)
                questText.text = "QUESTS";

            Button questButton = questButtonGo.GetComponent<Button>();

            RectTransform rootRt = prefabRoot.GetComponent<RectTransform>();
            if (rootRt != null && rootRt.sizeDelta.x < 440f)
                rootRt.sizeDelta = new Vector2(440f, rootRt.sizeDelta.y);

            SetObjectField(itemView, "_questButton", questButton);
            SetObjectField(itemView, "_questButtonText", questText);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, SessionPlayerListItemPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    static void WireSocialPanelCanvas(GameObject panelPrefab)
    {
        GameObject canvasRoot = PrefabUtility.LoadPrefabContents(SocialPanelCanvasPath);
        try
        {
            SessionPlayersPanelPresenter presenter = canvasRoot.GetComponent<SessionPlayersPanelPresenter>();
            if (presenter == null)
            {
                Debug.LogError("[GuiderQuestPrefabBuilder] SessionPlayersPanelPresenter missing on SocialPanelCanvas.");
                return;
            }

            GuiderPlayerQuestPanel existing = canvasRoot.GetComponentInChildren<GuiderPlayerQuestPanel>(true);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvasRoot.transform);
            panelInstance.name = "GuiderPlayerQuestPanel";
            panelInstance.SetActive(false);

            GuiderPlayerQuestPanel panel = panelInstance.GetComponent<GuiderPlayerQuestPanel>();
            SetObjectField(presenter, "_questPanel", panel);

            PrefabUtility.SaveAsPrefabAsset(canvasRoot, SocialPanelCanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvasRoot);
        }
    }

    static Transform CreateScrollList(Transform panel)
    {
        var scroll = new GameObject(
            "QuestScroll",
            typeof(RectTransform),
            typeof(ScrollRect),
            typeof(Image),
            typeof(Mask));
        scroll.transform.SetParent(panel, false);

        RectTransform scrollRt = scroll.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(16f, 16f);
        scrollRt.offsetMax = new Vector2(-16f, -80f);

        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        scroll.GetComponent<Mask>().showMaskGraphic = false;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scroll.transform, false);
        Stretch(viewport.GetComponent<RectTransform>(), 0f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return content.transform;
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().minHeight = 24f;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    static TextMeshProUGUI CreateAbsoluteTmp(
        Transform parent,
        string name,
        string text,
        float fontSize,
        FontStyles style,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    static Button CreateInlineButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minWidth = 72f;
        layout.preferredWidth = 72f;
        layout.preferredHeight = 28f;

        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        Stretch(textGo.GetComponent<RectTransform>(), 0f);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go.GetComponent<Button>();
    }

    static Button CreateAbsoluteButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        bool upperRight)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        Vector2 anchor = upperRight ? Vector2.one : new Vector2(0f, 1f);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = upperRight ? Vector2.one : new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        Stretch(textGo.GetComponent<RectTransform>(), 0f);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go.GetComponent<Button>();
    }

    static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    static void SetObjectField(Object target, string propertyName, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[GuiderQuestPrefabBuilder] Property '{propertyName}' not found on {target.GetType().Name}.");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
            return;

        string parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(assetFolderPath);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        if (!AssetDatabase.IsValidFolder(assetFolderPath))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
