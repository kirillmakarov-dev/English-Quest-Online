using System.Collections.Generic;
using EnglishKingdom.PortfolioDemo;
using EnglishKingdom.QuestSystem;
using Puzzle.Gameplay.MiniGames.DuolingoWordGame;
using Puzzle.Gameplay.MiniGames.LetterConnection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityServiceLocator;

namespace EnglishKingdom.Editor.PortfolioDemo
{
    [InitializeOnLoad]
    public static class PortfolioDemoSceneBuilder
    {
        private const string DemoRoot = "Assets/_PortfolioSlice/Demo";
        private const string ScenePath = DemoRoot + "/Scenes/PortfolioDemo.unity";
        private const string DataRoot = DemoRoot + "/Data";
        private const string PrefabRoot = DemoRoot + "/Prefabs";

        private const string LineMatchPrefabPath =
            "Assets/_PortfolioSlice/Art/Prefabs/GamePlay/LineMatch/LineMatch.prefab";
        private const string LetterOrderPrefabPath =
            "Assets/_PortfolioSlice/Art/Prefabs/UI/GamePlay/MiniGame/WordOrderingGame/LetterOrderGame.prefab";
        private const string WordOrderPrefabPath =
            "Assets/_PortfolioSlice/Art/Prefabs/UI/GamePlay/MiniGame/WordOrderingGame/WordOrderGame.prefab";
        private const string LineMatchDataPath =
            "Assets/_PortfolioSlice/Data/MiniGames/LineMatch/Letters_A_B.asset";
        private const string LetterOrderDataPath =
            "Assets/_PortfolioSlice/Art/Prefabs/UI/GamePlay/MiniGame/WordOrderingGame/LetterOrderingData.asset";
        private const string WordOrderDataPath =
            "Assets/_PortfolioSlice/Art/Prefabs/UI/GamePlay/MiniGame/WordOrderingGame/WordOrderingData.asset";

        static PortfolioDemoSceneBuilder()
        {
            EditorApplication.delayCall += BuildAutomaticallyIfNeeded;
        }

        [MenuItem("Tools/Portfolio Demo/Rebuild Test Scene")]
        public static void RebuildScene()
        {
            BuildScene(force: true);
        }

        private static void BuildAutomaticallyIfNeeded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || Application.isPlaying)
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                BuildScene(force: false);
        }

        private static void BuildScene(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return;

            EnsureFolder(DemoRoot);
            EnsureFolder(DemoRoot + "/Scenes");
            EnsureFolder(DataRoot);
            EnsureFolder(PrefabRoot);

            LineMatchQuestConfigSO lineConfig = CreateLineMatchConfig();
            LetterOrderingQuestConfigSO letterConfig = CreateLetterOrderingConfig();
            WordOrderingQuestConfigSO wordConfig = CreateWordOrderingConfig();
            DialogueNode dialogueStart = CreateDialogueData();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PortfolioDemo";

            CreateEnvironment();
            Camera camera = CreateCamera();
            CreateEventSystem();
            new GameObject("Service Locator Global").AddComponent<ServiceLocatorGlobal>();

            QuestObjectiveEventBus eventBus = new GameObject("Quest Objective Event Bus")
                .AddComponent<QuestObjectiveEventBus>();

            DialogueManager dialogueManager = CreateDialogueUi();
            PortfolioDemoHud hud = CreateHud(eventBus, dialogueManager);
            CreatePlayer(camera, hud);
            CreateNpc(dialogueStart);

            LetterConnectionBootstrap lineBootstrap =
                InstantiatePrefabComponent<LetterConnectionBootstrap>(LineMatchPrefabPath, "UI - Line Match");
            WordGameBootstrap letterBootstrap =
                InstantiatePrefabComponent<WordGameBootstrap>(LetterOrderPrefabPath, "UI - Letter Ordering");
            WordGameBootstrap wordBootstrap =
                InstantiatePrefabComponent<WordGameBootstrap>(WordOrderPrefabPath, "UI - Word Ordering");

            CreateMiniGameStation(
                "Line Match Station",
                new Vector3(-6f, 0.75f, 5f),
                new Color(0.12f, 0.65f, 0.72f),
                "line_match",
                "Play Line Match",
                lineConfig,
                null,
                lineBootstrap);

            CreateMiniGameStation(
                "Letter Ordering Station",
                new Vector3(0f, 0.75f, 7f),
                new Color(0.95f, 0.55f, 0.16f),
                "letter_ordering",
                "Play Letter Ordering",
                letterConfig,
                letterBootstrap,
                null);

            CreateMiniGameStation(
                "Word Ordering Station",
                new Vector3(6f, 0.75f, 5f),
                new Color(0.88f, 0.27f, 0.30f),
                "word_ordering",
                "Play Word Ordering",
                wordConfig,
                wordBootstrap,
                null);

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetAsFirstBuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"[PortfolioDemo] Test scene created: {ScenePath}");
        }

        private static void CreateEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.67f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.34f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.14f, 0.15f);

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.94f, 0.82f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Demo Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 3f);
            ground.transform.localScale = new Vector3(22f, 1f, 22f);
            ApplyColor(ground, new Color(0.12f, 0.2f, 0.18f));

            CreateDecoration(new Vector3(-9f, 0.5f, 8f), new Vector3(1.5f, 1f, 9f));
            CreateDecoration(new Vector3(9f, 0.5f, 8f), new Vector3(1.5f, 1f, 9f));
            CreateDecoration(new Vector3(0f, 0.25f, 11.5f), new Vector3(18f, 0.5f, 1f));
        }

        private static void CreateDecoration(Vector3 position, Vector3 scale)
        {
            GameObject decoration = GameObject.CreatePrimitive(PrimitiveType.Cube);
            decoration.name = "Boundary";
            decoration.transform.position = position;
            decoration.transform.localScale = scale;
            ApplyColor(decoration, new Color(0.18f, 0.3f, 0.25f));
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.09f, 0.12f);
            camera.fieldOfView = 55f;
            return camera;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void CreatePlayer(Camera camera, PortfolioDemoHud hud)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player Capsule";
            player.transform.position = new Vector3(0f, 1f, -5f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            ApplyColor(player, new Color(0.2f, 0.76f, 0.58f));

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = Vector3.zero;

            PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
            Transform holdPoint = new GameObject("Hold Point").transform;
            holdPoint.SetParent(player.transform);
            holdPoint.localPosition = new Vector3(0f, 0.6f, 0.8f);
            interaction.itemHolderPos = holdPoint;

            PortfolioDemoPlayerController controller =
                player.AddComponent<PortfolioDemoPlayerController>();
            player.AddComponent<PortfolioPlayerLockService>();

            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("followCamera").objectReferenceValue = camera;
            serializedController.FindProperty("hud").objectReferenceValue = hud;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PortfolioDemoHud CreateHud(
            QuestObjectiveEventBus eventBus,
            DialogueManager dialogueManager)
        {
            Canvas canvas = CreateCanvas("Portfolio Demo HUD", 10);
            TextMeshProUGUI title = CreateUiText(
                canvas.transform,
                "Title",
                "TEACHER ADVENTURE - SYSTEM TEST",
                28f,
                TextAlignmentOptions.TopLeft,
                new Vector2(24f, -20f),
                new Vector2(760f, 60f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            TextMeshProUGUI help = CreateUiText(
                canvas.transform,
                "Controls",
                "WASD - Move    E - Interact    Mouse - Mini-game UI",
                20f,
                TextAlignmentOptions.TopLeft,
                new Vector2(26f, -68f),
                new Vector2(820f, 45f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            TextMeshProUGUI prompt = CreateUiText(
                canvas.transform,
                "Interaction Prompt",
                string.Empty,
                30f,
                TextAlignmentOptions.Center,
                new Vector2(0f, 60f),
                new Vector2(700f, 70f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));
            prompt.color = new Color(1f, 0.88f, 0.35f);

            TextMeshProUGUI status = CreateUiText(
                canvas.transform,
                "System Status",
                "Ready. Complete a dialogue or mini-game.",
                20f,
                TextAlignmentOptions.TopRight,
                new Vector2(-24f, -24f),
                new Vector2(620f, 55f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));
            status.color = new Color(0.55f, 0.95f, 0.8f);

            PortfolioDemoHud hud = canvas.gameObject.AddComponent<PortfolioDemoHud>();
            SerializedObject serializedHud = new(hud);
            serializedHud.FindProperty("interactionPrompt").objectReferenceValue = prompt;
            serializedHud.FindProperty("statusText").objectReferenceValue = status;
            serializedHud.FindProperty("objectiveEventBus").objectReferenceValue = eventBus;
            serializedHud.FindProperty("dialogueManager").objectReferenceValue = dialogueManager;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            title.raycastTarget = false;
            help.raycastTarget = false;
            prompt.raycastTarget = false;
            status.raycastTarget = false;
            return hud;
        }

        private static DialogueManager CreateDialogueUi()
        {
            Canvas canvas = CreateCanvas("Dialogue Canvas", 30);
            GameObject panel = CreateUiPanel(
                canvas.transform,
                "Dialogue Panel",
                new Color(0.035f, 0.06f, 0.08f, 0.97f),
                new Vector2(0f, 30f),
                new Vector2(1000f, 350f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));

            TextMeshProUGUI nameText = CreateUiText(
                panel.transform,
                "Speaker Name",
                "Teacher Ada",
                30f,
                TextAlignmentOptions.TopLeft,
                new Vector2(40f, -30f),
                new Vector2(900f, 50f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));
            nameText.color = new Color(1f, 0.74f, 0.25f);

            TextMeshProUGUI dialogueText = CreateUiText(
                panel.transform,
                "Dialogue Text",
                string.Empty,
                26f,
                TextAlignmentOptions.TopLeft,
                new Vector2(40f, -95f),
                new Vector2(920f, 120f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            GameObject choices = new("Choices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            choices.transform.SetParent(panel.transform, false);
            RectTransform choicesRect = (RectTransform)choices.transform;
            choicesRect.anchorMin = new Vector2(0.5f, 0f);
            choicesRect.anchorMax = new Vector2(0.5f, 0f);
            choicesRect.pivot = new Vector2(0.5f, 0f);
            choicesRect.anchoredPosition = new Vector2(0f, 28f);
            choicesRect.sizeDelta = new Vector2(900f, 80f);
            HorizontalLayoutGroup layout = choices.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            GameObject managerObject = new("Dialogue Manager");
            DialogueManager manager = managerObject.AddComponent<DialogueManager>();
            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("dialoguePanel").objectReferenceValue = panel;
            serializedManager.FindProperty("nameText").objectReferenceValue = nameText;
            serializedManager.FindProperty("dialogueText").objectReferenceValue = dialogueText;
            serializedManager.FindProperty("choiceContainer").objectReferenceValue = choices.transform;
            serializedManager.FindProperty("choiceButtonPrefab").objectReferenceValue = CreateChoiceButtonPrefab();
            serializedManager.FindProperty("typingSpeed").floatValue = 0.015f;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return manager;
        }

        private static GameObject CreateChoiceButtonPrefab()
        {
            string path = PrefabRoot + "/DialogueChoiceButton.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            GameObject root = new("Dialogue Choice Button", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(310f, 62f);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.12f, 0.52f, 0.46f, 1f);

            TextMeshProUGUI label = CreateUiText(
                root.transform,
                "Label",
                "Continue",
                22f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateNpc(DialogueNode dialogueStart)
        {
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC - Teacher Ada";
            npc.transform.position = new Vector3(-6f, 1f, -1f);
            ApplyColor(npc, new Color(0.94f, 0.68f, 0.24f));

            NpcDialogueTrigger trigger = npc.AddComponent<NpcDialogueTrigger>();
            trigger.startingNode = dialogueStart;

            CreateWorldLabel(npc.transform, "NPC: Teacher Ada\nPress E to talk", new Vector3(0f, 1.65f, 0f));
        }

        private static void CreateMiniGameStation(
            string name,
            Vector3 position,
            Color color,
            string gameId,
            string prompt,
            QuestMiniGameConfigSO config,
            WordGameBootstrap wordBootstrap,
            LetterConnectionBootstrap lineBootstrap)
        {
            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = name;
            station.transform.position = position;
            station.transform.localScale = new Vector3(3.2f, 1.5f, 2f);
            ApplyColor(station, color);

            MiniGameWorldLaunchHost host = station.AddComponent<MiniGameWorldLaunchHost>();
            MiniGameWorldInteractable interactable = station.AddComponent<MiniGameWorldInteractable>();

            SerializedObject serializedHost = new(host);
            serializedHost.FindProperty("wordGameBootstrap").objectReferenceValue = wordBootstrap;
            serializedHost.FindProperty("lineMatchBootstrap").objectReferenceValue = lineBootstrap;
            serializedHost.FindProperty("fallbackConfig").objectReferenceValue = config;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedInteractable = new(interactable);
            serializedInteractable.FindProperty("gameId").stringValue = gameId;
            serializedInteractable.FindProperty("interactionPrompt").stringValue = prompt;
            serializedInteractable.FindProperty("launchHost").objectReferenceValue = host;
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel(station.transform, name.Replace(" Station", string.Empty), new Vector3(0f, 1.25f, 0f));
        }

        private static T InstantiatePrefabComponent<T>(string path, string objectName) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[PortfolioDemo] Missing prefab: {path}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = objectName;
            RepairMissingWordGameLayouts(instance);
            T component = instance.GetComponentInChildren<T>(true);
            if (component == null)
                Debug.LogError($"[PortfolioDemo] Prefab '{path}' has no {typeof(T).Name}.");

            return component;
        }

        private static void RepairMissingWordGameLayouts(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "tile container" && child.name != "slot container")
                    continue;

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                if (child.GetComponent<LayoutGroup>() != null)
                    continue;

                HorizontalLayoutGroup layout = child.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 20f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }

        private static LineMatchQuestConfigSO CreateLineMatchConfig()
        {
            const string path = DataRoot + "/LineMatchQuestConfig.asset";
            LineMatchQuestConfigSO asset = LoadOrCreate<LineMatchQuestConfigSO>(path);
            SerializedObject serialized = new(asset);
            serialized.FindProperty("gameId").stringValue = "line_match";
            serialized.FindProperty("levelConfig").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<LetterConnectionLevelConfigSO>(LineMatchDataPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static LetterOrderingQuestConfigSO CreateLetterOrderingConfig()
        {
            const string path = DataRoot + "/LetterOrderingQuestConfig.asset";
            LetterOrderingQuestConfigSO asset = LoadOrCreate<LetterOrderingQuestConfigSO>(path);
            SerializedObject serialized = new(asset);
            serialized.FindProperty("gameId").stringValue = "letter_ordering";
            serialized.FindProperty("data").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<LetterOrderingDataSO>(LetterOrderDataPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static WordOrderingQuestConfigSO CreateWordOrderingConfig()
        {
            const string path = DataRoot + "/WordOrderingQuestConfig.asset";
            WordOrderingQuestConfigSO asset = LoadOrCreate<WordOrderingQuestConfigSO>(path);
            SerializedObject serialized = new(asset);
            serialized.FindProperty("gameId").stringValue = "word_ordering";
            serialized.FindProperty("data").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<WordOrderingDataSO>(WordOrderDataPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static DialogueNode CreateDialogueData()
        {
            const string startPath = DataRoot + "/Dialogue_Teacher_Start.asset";
            const string systemsPath = DataRoot + "/Dialogue_Teacher_Systems.asset";

            DialogueNode systems = LoadOrCreate<DialogueNode>(systemsPath);
            systems.speakerName = "Teacher Ada";
            systems.dialogueText =
                "This scene uses Service Locator, dialogue ScriptableObjects, quest events, and reusable mini-game presenters.";
            systems.actionType = DialogueActionType.None;
            systems.choices = new List<DialogueChoice>();
            EditorUtility.SetDirty(systems);

            DialogueNode start = LoadOrCreate<DialogueNode>(startPath);
            start.speakerName = "Teacher Ada";
            start.dialogueText =
                "Welcome to the Teacher Adventure portfolio lab. What would you like to verify?";
            start.actionType = DialogueActionType.None;
            start.choices = new List<DialogueChoice>
            {
                new() { choiceText = "Explain the architecture", nextNode = systems },
                new() { choiceText = "Everything works. Goodbye!", nextNode = null }
            };
            EditorUtility.SetDirty(start);
            return start;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Canvas CreateCanvas(string name, int sortingOrder)
        {
            GameObject canvasObject = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject CreateUiPanel(
            Transform parent,
            string name,
            Color color,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static TextMeshProUGUI CreateUiText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(
                Mathf.Approximately(anchorMin.x, 1f) ? 1f : Mathf.Approximately(anchorMin.x, 0f) ? 0f : 0.5f,
                Mathf.Approximately(anchorMin.y, 1f) ? 1f : Mathf.Approximately(anchorMin.y, 0f) ? 0f : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }

        private static void CreateWorldLabel(Transform parent, string text, Vector3 localPosition)
        {
            GameObject labelObject = new("Label", typeof(TextMeshPro));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(25f, 180f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.18f;
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 5f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(12f, 4f);
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader)
            {
                color = color,
                name = target.name + " Material"
            };
            renderer.sharedMaterial = material;
        }

        private static void SetAsFirstBuildScene()
        {
            List<EditorBuildSettingsScene> scenes = new()
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath)
                    scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
