using EnglishQuest.PortfolioDemo;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EnglishQuest.Editor
{
    [InitializeOnLoad]
    internal static class PortfolioDemoSceneExitMaterializer
    {
        private const string SceneName = "PortfolioDemo";
        private const string StudyCircleName = "Optional Co-op Study Circle";
        private const string BarrierRootName = "Way to next level";
        private const string TriggerName = "Next level triger";
        private const string RevealCameraName = "Way to Next Level Camera";
        private const string MaterialFolder = "Assets/_PortfolioSlice/Demo/Materials";
        private const string MaterialPath = MaterialFolder + "/OptionalCoopStudyCircle.mat";
        private const string CompletionPrefabFolder = "Assets/_PortfolioSlice/Demo/Prefabs/UI";
        private const string CompletionPrefabPath = CompletionPrefabFolder + "/PortfolioNextLevelCompletion.prefab";

        static PortfolioDemoSceneExitMaterializer()
        {
            EditorApplication.delayCall += MaterializeActivePortfolioScene;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Tools/English Quest/Portfolio Demo/Materialize Study Circle And Level Exit")]
        private static void MaterializeFromMenu()
        {
            MaterializeActivePortfolioScene();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += MaterializeActivePortfolioScene;
        }

        private static void MaterializeActivePortfolioScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName)
                return;

            bool changed = false;
            PortfolioOptionalCoopStudyCircle studyCircle = EnsureStudyCircle(scene, ref changed);
            PortfolioDemoHud hud = FindSceneComponent<PortfolioDemoHud>(scene);
            GameObject barrierRoot = FindRoot(scene, BarrierRootName);
            GameObject triggerObject = FindRoot(scene, TriggerName);
            CinemachineCamera revealCamera = FindNamedSceneComponent<CinemachineCamera>(scene, RevealCameraName);
            PortfolioNextLevelCompletionView completionView =
                FindSceneComponent<PortfolioNextLevelCompletionView>(scene);

            if (barrierRoot == null || triggerObject == null || revealCamera == null || completionView == null)
            {
                Debug.LogWarning(
                    "Portfolio demo exit materializer could not find the authored level-exit objects or Pause And Completion Menu prefab instance.");
                return;
            }

            PortfolioNextLevelSequence sequence = barrierRoot.GetComponent<PortfolioNextLevelSequence>();
            if (sequence == null)
            {
                sequence = barrierRoot.AddComponent<PortfolioNextLevelSequence>();
                changed = true;
            }

            PortfolioNextLevelTrigger trigger = triggerObject.GetComponent<PortfolioNextLevelTrigger>();
            if (trigger == null)
            {
                trigger = triggerObject.AddComponent<PortfolioNextLevelTrigger>();
                changed = true;
            }

            changed |= SetReference(sequence, "barriersRoot", barrierRoot.transform);
            changed |= SetReference(sequence, "revealCamera", revealCamera);
            changed |= SetReference(sequence, "exitTrigger", trigger);
            changed |= SetReference(sequence, "hud", hud);
            changed |= SetReference(trigger, "sequence", sequence);
            changed |= SetReference(trigger, "completionView", completionView);

            if (!changed)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[PortfolioDemo] Scene-authored study circle and next-level sequence are configured. Study circle: '{studyCircle.name}'.",
                barrierRoot);
        }

        private static PortfolioNextLevelCompletionView EnsureCompletionViewPrefab()
        {
            PortfolioNextLevelCompletionView existing =
                AssetDatabase.LoadAssetAtPath<PortfolioNextLevelCompletionView>(CompletionPrefabPath);
            if (existing != null)
                return existing;

            EnsureAssetFolder(CompletionPrefabFolder);

            GameObject root = new("Portfolio Next Level Completion", typeof(RectTransform));
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            PortfolioNextLevelCompletionView view = root.AddComponent<PortfolioNextLevelCompletionView>();
            Image background = CreateImage(root.transform, "Fade Background", new Color(0.015f, 0.02f, 0.025f, 1f));
            Stretch(background.rectTransform);

            RectTransform content = CreateRect(root.transform, "Content");
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(1100f, 420f);

            TextMeshProUGUI eyebrow = CreateText(content, "Eyebrow", 30f, FontStyles.Bold);
            SetAnchoredRect(eyebrow.rectTransform, new Vector2(0f, 125f), new Vector2(1000f, 55f));
            eyebrow.color = new Color(0.31f, 0.82f, 0.78f, 1f);
            eyebrow.characterSpacing = 5f;

            TextMeshProUGUI title = CreateText(content, "Title", 60f, FontStyles.Bold);
            SetAnchoredRect(title.rectTransform, new Vector2(0f, 25f), new Vector2(1080f, 140f));
            title.color = Color.white;
            title.enableAutoSizing = true;
            title.fontSizeMin = 38f;
            title.fontSizeMax = 60f;

            TextMeshProUGUI support = CreateText(content, "Support Message", 28f, FontStyles.Normal);
            SetAnchoredRect(support.rectTransform, new Vector2(0f, -105f), new Vector2(980f, 100f));
            support.color = new Color(0.78f, 0.84f, 0.86f, 1f);

            SetReference(view, "rootCanvas", canvas);
            SetReference(view, "canvasGroup", canvasGroup);
            SetReference(view, "eyebrowText", eyebrow);
            SetReference(view, "titleText", title);
            SetReference(view, "supportText", support);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CompletionPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return prefab.GetComponent<PortfolioNextLevelCompletionView>();
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            float fontSize,
            FontStyles fontStyle)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(Transform parent, string objectName)
        {
            GameObject child = new(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredRect(RectTransform rectTransform, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static PortfolioOptionalCoopStudyCircle EnsureStudyCircle(Scene scene, ref bool changed)
        {
            PortfolioOptionalCoopStudyCircle circle = FindSceneComponent<PortfolioOptionalCoopStudyCircle>(scene);
            GameObject root;
            if (circle == null)
            {
                root = FindRoot(scene, StudyCircleName);
                if (root == null)
                {
                    root = new GameObject(StudyCircleName);
                    root.transform.position = new Vector3(0f, 0.03f, 2.2f);
                    SceneManager.MoveGameObjectToScene(root, scene);
                    changed = true;
                }

                circle = root.AddComponent<PortfolioOptionalCoopStudyCircle>();
                changed = true;
            }
            else
            {
                root = circle.gameObject;
            }

            MeshRenderer ring = EnsureRing(root.transform, ref changed);
            TextMeshPro label = EnsureLabel(root.transform, ref changed);
            changed |= SetReference(circle, "anchor", root.transform);
            changed |= SetReference(circle, "ringRenderer", ring);
            changed |= SetReference(circle, "label", label);
            return circle;
        }

        private static MeshRenderer EnsureRing(Transform root, ref bool changed)
        {
            Transform existing = root.Find("Ring");
            GameObject ring;
            if (existing == null)
            {
                ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Ring";
                ring.transform.SetParent(root, false);
                ring.transform.localScale = new Vector3(4.5f, 0.02f, 4.5f);
                Object.DestroyImmediate(ring.GetComponent<Collider>());
                changed = true;
            }
            else
            {
                ring = existing.gameObject;
            }

            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            Material material = EnsureStudyCircleMaterial();
            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                changed = true;
            }

            return renderer;
        }

        private static TextMeshPro EnsureLabel(Transform root, ref bool changed)
        {
            Transform existing = root.Find("Label");
            TextMeshPro label;
            if (existing == null)
            {
                GameObject labelObject = new("Label", typeof(TextMeshPro));
                labelObject.transform.SetParent(root, false);
                labelObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                labelObject.transform.localRotation = Quaternion.Euler(25f, 180f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.18f;
                label = labelObject.GetComponent<TextMeshPro>();
                label.fontSize = 4.5f;
                label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(15f, 5f);
                label.textWrappingMode = TextWrappingModes.Normal;
                label.text = "Study Circle\nOptional co-op marker";
                changed = true;
            }
            else
            {
                label = existing.GetComponent<TextMeshPro>();
                if (label == null)
                {
                    label = existing.gameObject.AddComponent<TextMeshPro>();
                    changed = true;
                }
            }

            return label;
        }

        private static Material EnsureStudyCircleMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/_PortfolioSlice/Demo", "Materials");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                name = "Optional Co-op Study Circle",
                color = new Color(0.18f, 0.5f, 0.55f, 0.92f)
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static bool SetReference(Object target, string propertyName, Object value)
        {
            if (target == null)
                return false;

            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component.gameObject.scene == scene)
                    return component;
            }

            return null;
        }

        private static T FindNamedSceneComponent<T>(Scene scene, string objectName) where T : Component
        {
            foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component.gameObject.scene == scene && component.name.Trim() == objectName)
                    return component;
            }

            return null;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.Trim() == objectName)
                    return root;
            }

            return null;
        }
    }
}
