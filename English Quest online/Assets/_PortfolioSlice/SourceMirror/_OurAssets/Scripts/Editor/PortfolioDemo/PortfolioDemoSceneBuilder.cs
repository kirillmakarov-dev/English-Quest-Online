using System;
using System.Collections.Generic;
using EnglishQuest.PortfolioDemo;
using EnglishQuest.QuestSystem;
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

namespace EnglishQuest.Editor.PortfolioDemo
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
        private const string NetworkPlayerPrefabPath =
            "Assets/_PortfolioSlice/Art/Prefabs/Player.prefab";
        private const string StarterFollowCameraPrefabPath =
            "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
        private const string NetworkSessionProfilePath =
            DataRoot + "/QuestLines/Shared_MVP_Catalogs/OpenWorldNetworkSessionProfile.asset";
        private const string InteractionZoneName = "Interaction Zone";
        private const string LineMatchDataPath =
            "Assets/_PortfolioSlice/Demo/Data/QuestLines/01_TeacherAda_FirstQuestline/LineMatch_Letters_A_B.asset";
        private const string LetterOrderDataPath =
            "Assets/_PortfolioSlice/Demo/Data/QuestLines/02_CoachBen_SecondQuestline/LetterOrderingData.asset";
        private const string WordOrderDataPath =
            "Assets/_PortfolioSlice/Demo/Data/QuestLines/03_GuideNora_ThirdQuestline/WordOrderingData.asset";

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
            DialogueNode lettersDialogue = CreateLetterIntroDialogue();
            DialogueNode missingLetterDialogue = CreateMissingLetterDialogue();
            DialogueNode questionDialogue = CreateQuestionDialogue();
            QuestLineRegistrySO questRegistry = CreateQuestRegistry(
                lettersDialogue,
                missingLetterDialogue,
                questionDialogue,
                lineConfig,
                letterConfig,
                wordConfig);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PortfolioDemo";

            CreateEnvironment();
            CreateCamera();
            CreateEventSystem();
            new GameObject("Service Locator Global").AddComponent<ServiceLocatorGlobal>();

            QuestObjectiveEventBus eventBus = new GameObject("Quest Objective Event Bus")
                .AddComponent<QuestObjectiveEventBus>();

            DialogueManager dialogueManager = CreateDialogueUi();
            CreateHud(eventBus, dialogueManager);
            CreateNetwork();
            CreateQuestSystems(questRegistry);
            CreateNpc(
                "NPC - Teacher Ada",
                new Vector3(-5f, 1f, -1f),
                new Color(0.94f, 0.68f, 0.24f),
                "NPC: Teacher Ada\nLesson 1 - Two letters",
                "teacher_ada",
                questRegistry.questLines[0]);
            CreateNpc(
                "NPC - Coach Ben",
                new Vector3(0f, 1f, -1f),
                new Color(0.28f, 0.72f, 0.94f),
                "NPC: Coach Ben\nLesson 2 - Missing letter",
                "coach_ben",
                questRegistry.questLines[1]);
            CreateNpc(
                "NPC - Guide Nora",
                new Vector3(7f, 1f, -1f),
                new Color(0.72f, 0.33f, 0.92f),
                "NPC: Guide Nora\nLesson 3 - Choose the word",
                "guide_nora",
                questRegistry.questLines[2]);
            CreateOptionalCoopStudyCircle();

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
            AddComponentByTypeName(cameraObject, "Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
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

        private static void CreateNetwork()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPlayerPrefabPath);
            NetworkSessionProfile profile =
                AssetDatabase.LoadAssetAtPath<NetworkSessionProfile>(NetworkSessionProfilePath);

            if (playerPrefab == null || profile == null)
            {
                Debug.LogError("[PortfolioDemo] Network player prefab or session profile is missing.");
                return;
            }

            GameObject network = new("Network");
            GameNetworkManager manager = network.AddComponent<GameNetworkManager>();
            Component sceneManager = AddComponentByTypeName(
                network,
                "EnglishQuestNetworkSceneManager, _Project");
            PlayerSpawnCoordinator spawnCoordinator =
                network.AddComponent<PlayerSpawnCoordinator>();
            PortfolioNetworkAutoStart autoStart =
                network.AddComponent<PortfolioNetworkAutoStart>();

            if (sceneManager == null)
            {
                Debug.LogError("[PortfolioDemo] EnglishQuestNetworkSceneManager type is unavailable.");
                UnityEngine.Object.DestroyImmediate(network);
                return;
            }

            SerializedObject managerData = new(manager);
            managerData.FindProperty("_sceneManager").objectReferenceValue = sceneManager;
            managerData.FindProperty("_openWorldProfile").objectReferenceValue = profile;
            managerData.ApplyModifiedPropertiesWithoutUndo();

            SetNetworkPrefabReference(
                spawnCoordinator,
                "_defaultPlayerPrefab",
                AssetDatabase.AssetPathToGUID(NetworkPlayerPrefabPath));

            SerializedObject autoStartData = new(autoStart);
            autoStartData.FindProperty("sessionProfile").objectReferenceValue = profile;
            autoStartData.FindProperty("autoStart").boolValue = true;
            autoStartData.ApplyModifiedPropertiesWithoutUndo();

            CreateSpawnPoint("Spawn Point - Player One", new Vector3(-1.5f, 0.03f, -3.53f));
            CreateSpawnPoint("Spawn Point - Player Two", new Vector3(1.5f, 0.03f, -3.53f));
            SetupStarterAssetsCamera(null);
        }

        private static void CreateSpawnPoint(string objectName, Vector3 position)
        {
            GameObject spawnPoint = new(objectName);
            spawnPoint.transform.position = position;
            spawnPoint.AddComponent<PlayerSpawnPoint>();
        }

        private static void CreateOptionalCoopStudyCircle()
        {
            GameObject studyCircle = new("Optional Co-op Study Circle");
            studyCircle.transform.position = new Vector3(0f, 0.03f, 2.2f);
            studyCircle.AddComponent<PortfolioOptionalCoopStudyCircle>();

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(studyCircle.transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(4.5f, 0.02f, 4.5f);

            Collider ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
                UnityEngine.Object.DestroyImmediate(ringCollider);

            ApplyColor(ring, new Color(0.18f, 0.5f, 0.55f));
            CreateWorldLabel(
                studyCircle.transform,
                "Optional Co-op\nStudy Circle\n(Not Required)",
                new Vector3(0f, 1.35f, 0f));
        }

        private static PortfolioDemoHud CreateHud(
            QuestObjectiveEventBus eventBus,
            DialogueManager dialogueManager)
        {
            Canvas canvas = CreateCanvas("Portfolio Demo HUD", 10);
            TextMeshProUGUI title = CreateUiText(
                canvas.transform,
                "Title",
                "ENGLISH QUEST MVP - OPEN WORLD + QUEST CHAINS + OPTIONAL MULTIPLAYER",
                28f,
                TextAlignmentOptions.TopLeft,
                new Vector2(24f, -20f),
                new Vector2(760f, 60f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            TextMeshProUGUI help = CreateUiText(
                canvas.transform,
                "Controls",
                "WASD - Move    Mouse - Look    Space - Jump    E - Interact    Optional 2 Players via Photon Fusion",
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
            dialogueText.raycastTarget = false;

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
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateNpc(
            string objectName,
            Vector3 position,
            Color color,
            string labelText,
            string npcId,
            QuestLineSO questLine)
        {
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = objectName;
            npc.transform.position = position;
            ApplyColor(npc, color);

            NpcQuestGiver questGiver = npc.AddComponent<NpcQuestGiver>();
            SerializedObject serialized = new(questGiver);
            serialized.FindProperty("npcId").stringValue = npcId;
            serialized.FindProperty("questLine").objectReferenceValue = questLine;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldLabel(npc.transform, labelText, new Vector3(0f, 1.65f, 0f));
        }

        private static void CreateQuestSystems(QuestLineRegistrySO questRegistry)
        {
            QuestManager questManager = new GameObject("Quest Manager").AddComponent<QuestManager>();
            questManager.gameObject.AddComponent<EnglishQuest.PortfolioDemo.PortfolioQuestProgressController>();
            SerializedObject serializedManager = new(questManager);
            serializedManager.FindProperty("loadQuestState").boolValue = true;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            GameObject registrarObject = new("Quest Line Registrar");
            QuestLineRegistrar registrar = registrarObject.AddComponent<QuestLineRegistrar>();
            SerializedObject serialized = new(registrar);
            serialized.FindProperty("registry").objectReferenceValue = questRegistry;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

            instance.SetActive(false);
            return component;
        }

        private static GameObject InstantiatePrefabRoot(string path, string objectName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[PortfolioDemo] Missing prefab: {path}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = objectName;
            return instance;
        }

        private static void SetupStarterAssetsCamera(Transform playerRoot)
        {
            Transform target = playerRoot != null
                ? playerRoot.Find("CinemachineCameraTarget")
                : null;
            if (playerRoot != null && target == null)
            {
                Debug.LogWarning("[PortfolioDemo] Starter Assets player is missing CinemachineCameraTarget.");
                return;
            }

            GameObject followCameraObject = GameObject.Find("PlayerFollowCamera");
            if (followCameraObject == null)
            {
                followCameraObject = InstantiatePrefabRoot(StarterFollowCameraPrefabPath, "PlayerFollowCamera");
                if (followCameraObject == null)
                    return;
            }

            Component followCamera = FindComponentByTypeName(followCameraObject, "CinemachineCamera");
            if (followCamera == null)
            {
                followCamera = FindComponentByTypeNameInChildren(followCameraObject.transform, "CinemachineCamera");
            }

            if (followCamera == null)
            {
                Debug.LogWarning("[PortfolioDemo] Starter Assets follow camera prefab has no CinemachineCamera.");
                return;
            }

            SetIntMember(followCamera, "Priority", 10);
            if (target != null)
            {
                SetObjectMember(followCamera, "Follow", target);
                SetObjectMember(followCamera, "LookAt", target);
            }
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

        private static QuestLineRegistrySO CreateQuestRegistry(
            DialogueNode lettersDialogue,
            DialogueNode missingLetterDialogue,
            DialogueNode questionDialogue,
            LineMatchQuestConfigSO lineConfig,
            LetterOrderingQuestConfigSO letterConfig,
            WordOrderingQuestConfigSO wordConfig)
        {
            QuestDefinitionSO teacherQuest = CreateQuestDefinition(
                DataRoot + "/Quest_TeacherAda_Letters.asset",
                "Quest_TeacherAda_Letters",
                "quest_teacher_ada_letters",
                "Lesson 1 - Two letters",
                "Complete the first mini-game to learn the opening letters and unlock the second NPC.",
                "teacher_ada",
                100,
                lettersDialogue,
                lineConfig,
                "line_match",
                "Match the first two letters.");

            QuestDefinitionSO coachQuest = CreateQuestDefinition(
                DataRoot + "/Quest_CoachBen_MissingLetter.asset",
                "Quest_CoachBen_MissingLetter",
                "quest_coach_ben_missing_letter",
                "Lesson 2 - Missing letter",
                "Fill the missing letter and learn a new word before the next NPC unlocks.",
                "coach_ben",
                125,
                missingLetterDialogue,
                letterConfig,
                "letter_ordering",
                "Complete the missing-letter mini-game.");

            QuestDefinitionSO guideQuest = CreateQuestDefinition(
                DataRoot + "/Quest_GuideNora_ChooseWord.asset",
                "Quest_GuideNora_ChooseWord",
                "quest_guide_nora_choose_word",
                "Lesson 3 - Choose the word",
                "Pick the correct word from three options and complete the last MVP step.",
                "guide_nora",
                150,
                questionDialogue,
                wordConfig,
                "word_ordering",
                "Choose the correct word from three answers.");

            QuestLineSO teacherLine = CreateQuestLine(
                DataRoot + "/QuestLine_TeacherAda.asset",
                "QuestLine_TeacherAda",
                "line_teacher_ada",
                "teacher_ada",
                null,
                "Teacher Ada",
                "Learn the first two letters.",
                teacherQuest);

            QuestLineSO coachLine = CreateQuestLine(
                DataRoot + "/QuestLine_CoachBen.asset",
                "QuestLine_CoachBen",
                "line_coach_ben",
                "coach_ben",
                "line_teacher_ada",
                "Coach Ben",
                "Fill the missing letter and learn a new word.",
                coachQuest);

            QuestLineSO guideLine = CreateQuestLine(
                DataRoot + "/QuestLine_GuideNora.asset",
                "QuestLine_GuideNora",
                "line_guide_nora",
                "guide_nora",
                "line_coach_ben",
                "Guide Nora",
                "Choose the correct word from three options.",
                guideQuest);

            QuestCatalogSO questCatalog = LoadOrCreate<QuestCatalogSO>(DataRoot + "/QuestCatalog_MVP.asset");
            questCatalog.definitions = new List<QuestDefinitionSO> { teacherQuest, coachQuest, guideQuest };
            EditorUtility.SetDirty(questCatalog);

            QuestWorldCatalogSetSO worldCatalogSet =
                LoadOrCreate<QuestWorldCatalogSetSO>(DataRoot + "/QuestWorldCatalogSet_MVP.asset");
            worldCatalogSet.interactableCatalog = CreateInteractableCatalog();
            worldCatalogSet.questCatalog = questCatalog;
            EditorUtility.SetDirty(worldCatalogSet);

            QuestLineRegistrySO registry = LoadOrCreate<QuestLineRegistrySO>(DataRoot + "/QuestLineRegistry_MVP.asset");
            registry.questLines = new List<QuestLineSO> { teacherLine, coachLine, guideLine };
            registry.questCatalog = questCatalog;
            registry.worldCatalogSet = worldCatalogSet;
            registry.questRuntimeShellPrefab = null;
            registry.activeQuestJournalCanvasPrefab = null;
            EditorUtility.SetDirty(registry);
            return registry;
        }

        private static InteractableCatalogSO CreateInteractableCatalog()
        {
            InteractableCatalogSO catalog =
                LoadOrCreate<InteractableCatalogSO>(DataRoot + "/InteractableCatalog_MVP.asset");

            SerializedObject serialized = new(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 3;
            SetInteractableEntry(entries.GetArrayElementAtIndex(0), "line_match", "Line Match");
            SetInteractableEntry(entries.GetArrayElementAtIndex(1), "letter_ordering", "Letter Ordering");
            SetInteractableEntry(entries.GetArrayElementAtIndex(2), "word_ordering", "Word Ordering");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void SetInteractableEntry(SerializedProperty entry, string id, string displayName)
        {
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("kind").enumValueIndex = (int)InteractableCatalogKind.MiniGame;
            entry.FindPropertyRelative("displayName").stringValue = displayName;
        }

        private static QuestDefinitionSO CreateQuestDefinition(
            string path,
            string assetName,
            string questId,
            string displayName,
            string description,
            string giverNpcId,
            int xpReward,
            DialogueNode dialogue,
            QuestMiniGameConfigSO miniGameConfig,
            string targetId,
            string objectiveDisplayText)
        {
            QuestDefinitionSO asset = LoadOrCreate<QuestDefinitionSO>(path);
            asset.name = assetName;
            asset.id = questId;
            asset.displayName = displayName;
            asset.description = description;
            asset.levelRequired = 0;
            asset.giverNpcId = giverNpcId;
            asset.prerequisiteQuest = null;
            asset.prerequisiteQuestId = null;
            asset.waitForNpcTurnIn = false;
            asset.rewards ??= new QuestRewardData();
            asset.rewards.xp = xpReward;
            asset.rewards.items = new List<QuestRewardItemData>();
            asset.rewardDefinition = null;
            asset.startDialogue = dialogue;
            asset.inProgressDialogue = dialogue;
            asset.turnInDialogue = dialogue;
            asset.cannotStartDialogue = null;
            asset.alreadyFinishedDialogue = dialogue;
            asset.objectives = new List<QuestObjectiveDefinition>
            {
                new()
                {
                    type = QuestObjectiveType.CompleteMiniGame,
                    targetId = targetId,
                    count = 1,
                    displayText = objectiveDisplayText,
                    parameters = new List<QuestObjectiveParameter>(),
                    miniGameConfig = miniGameConfig,
                    requiredItemId = 0,
                    dialogue = null,
                    dialogueAfterFinished = null,
                    stepReward = null,
                    showStepRewardPopup = true
                }
            };
            asset.authoringCatalogSet = null;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static QuestLineSO CreateQuestLine(
            string path,
            string assetName,
            string lineId,
            string npcId,
            string prerequisiteLineId,
            string displayName,
            string theme,
            QuestDefinitionSO quest)
        {
            QuestLineSO asset = LoadOrCreate<QuestLineSO>(path);
            asset.name = assetName;
            asset.lineId = lineId;
            asset.npcId = npcId;
            asset.prerequisiteLineId = prerequisiteLineId;
            asset.displayName = displayName;
            asset.theme = theme;
            asset.quests = new List<QuestDefinitionSO> { quest };
            asset.worldCatalogSet = null;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static DialogueNode CreateLetterIntroDialogue()
        {
            const string path = DataRoot + "/Dialogue_Lesson_01_Letters.asset";

            return CreateDialogueNode(
                path,
                "Teacher Ada",
                "Lesson 1: we start with two letters. Match them first, then the second NPC will unlock.",
                "Got it");
        }

        private static DialogueNode CreateMissingLetterDialogue()
        {
            const string path = DataRoot + "/Dialogue_Lesson_02_MissingLetter.asset";

            return CreateDialogueNode(
                path,
                "Coach Ben",
                "Lesson 2: fill the missing letter and learn new English words along the way.",
                "Continue");
        }

        private static DialogueNode CreateQuestionDialogue()
        {
            const string path = DataRoot + "/Dialogue_Lesson_03_ChooseWord.asset";

            return CreateDialogueNode(
                path,
                "Guide Nora",
                "Lesson 3: I show you a word, and you choose the correct answer from three options.",
                "Let's do it");
        }

        private static DialogueNode CreateDialogueNode(
            string path,
            string speakerName,
            string dialogueText,
            string choiceText)
        {
            DialogueNode node = LoadOrCreate<DialogueNode>(path);
            node.speakerName = speakerName;
            node.dialogueText = dialogueText;
            node.actionType = DialogueActionType.None;
            node.choices = new List<DialogueChoice>
            {
                new() { choiceText = choiceText, nextNode = null }
            };
            EditorUtility.SetDirty(node);
            return node;
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

        private static Component AddComponentByTypeName(GameObject target, string typeName)
        {
            if (target == null)
                return null;

            Type type = Type.GetType(typeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
                return null;

            return target.AddComponent(type);
        }

        private static Component FindComponentByTypeName(GameObject target, string typeName)
        {
            if (target == null)
                return null;

            MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == typeName)
                    return behaviour;
            }

            return null;
        }

        private static Component FindComponentByTypeNameInChildren(Transform root, string typeName)
        {
            if (root == null)
                return null;

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == typeName)
                    return behaviour;
            }

            return null;
        }

        private static void EnsureInteractionZone(GameObject player)
        {
            if (player == null)
                return;

            Transform zone = player.transform.Find(InteractionZoneName);
            if (zone == null)
            {
                GameObject zoneObject = new(InteractionZoneName);
                zoneObject.transform.SetParent(player.transform, false);
                zoneObject.transform.localPosition = Vector3.zero;
                zone = zoneObject.transform;
            }

            SphereCollider collider = zone.GetComponent<SphereCollider>();
            if (collider == null)
                collider = zone.gameObject.AddComponent<SphereCollider>();

            zone.gameObject.layer = 0;
            collider.isTrigger = true;
            collider.radius = 2.4f;

            Rigidbody rigidbody = zone.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = zone.gameObject.AddComponent<Rigidbody>();

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;

            if (zone.GetComponent<InteractionZoneTrigger>() == null)
                zone.gameObject.AddComponent<InteractionZoneTrigger>();

            if (zone.GetComponent<PlayerInteraction>() == null)
            {
                PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
                InteractionZoneTrigger trigger = zone.GetComponent<InteractionZoneTrigger>();
                if (interaction != null && trigger != null)
                    SetObjectMember(trigger, "playerInteractionScript", interaction);
            }
        }

        private static void SetIntMember(Component target, string memberName, int value)
        {
            if (target == null)
                return;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            Type type = target.GetType();
            System.Reflection.PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite && property.PropertyType == typeof(int))
            {
                property.SetValue(target, value);
                return;
            }

            System.Reflection.FieldInfo field = type.GetField(memberName, flags);
            if (field != null && field.FieldType == typeof(int))
                field.SetValue(target, value);
        }

        private static void SetObjectMember(Component target, string memberName, object value)
        {
            if (target == null)
                return;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            Type type = target.GetType();
            System.Reflection.PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return;
            }

            System.Reflection.FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
                field.SetValue(target, value);
        }

        private static void SetNetworkPrefabReference(
            Component target,
            string fieldName,
            string assetGuid)
        {
            if (target == null || string.IsNullOrEmpty(assetGuid))
                return;

            const System.Reflection.BindingFlags fieldFlags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic;
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, fieldFlags);
            if (field == null)
                return;

            const System.Reflection.BindingFlags methodFlags =
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public;
            System.Reflection.MethodInfo parse = field.FieldType.GetMethod(
                "Parse",
                methodFlags,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);

            if (parse == null)
                return;

            field.SetValue(target, parse.Invoke(null, new object[] { assetGuid }));
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

