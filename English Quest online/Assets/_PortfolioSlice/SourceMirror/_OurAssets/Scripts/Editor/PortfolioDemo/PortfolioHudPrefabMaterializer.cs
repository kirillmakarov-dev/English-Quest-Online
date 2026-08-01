using EnglishQuest.PortfolioDemo;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishQuest.Editor.PortfolioDemo
{
    /// <summary>
    /// Saves the currently configured PortfolioDemo UI scene objects as prefabs.
    /// This tool intentionally does not create visual UI or assign sprites: the scene/prefab is the source of truth.
    /// </summary>
    public static class PortfolioHudPrefabMaterializer
    {
        private const string PrefabFolder = "Assets/_PortfolioSlice/Art/Prefabs/UI/PortfolioHUD";

        [MenuItem("Tools/English Quest/UI/Materialize Portfolio HUD Prefabs")]
        public static void MaterializePortfolioHud()
        {
            GameObject hud = GameObject.Find("Portfolio Demo HUD");
            if (hud == null)
                throw new System.InvalidOperationException("Portfolio Demo HUD was not found in the active scene.");

            EnsureFolder(PrefabFolder);

            GameObject header = FindExisting(hud.transform, "Header Presentation Card");
            GameObject briefing = FindExisting(hud.transform, "Demo Briefing Panel");
            GameObject players = FindExisting(hud.transform, "Multiplayer Players Panel");
            GameObject completion = FindExisting(hud.transform, "Completion Panel");
            GameObject transition = FindExisting(hud.transform, "MiniGame Transition Overlay");
            GameObject dialogue = FindDialoguePanel();

            ConnectPrefab(header, "PortfolioHUD_Header.prefab");
            ConnectPrefab(briefing, "PortfolioHUD_DemoBriefing.prefab");
            ConnectPrefab(players, "PortfolioHUD_MultiplayerPlayers.prefab");
            ConnectPrefab(completion, "PortfolioHUD_Completion.prefab");
            ConnectPrefab(transition, "PortfolioHUD_MiniGameTransition.prefab");
            ConnectPrefab(dialogue, "PortfolioHUD_DialoguePanel.prefab");

            WireHudReferences(hud, header, briefing, players, completion, transition);
            WireDialogueReferences(dialogue);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject FindExisting(Transform parent, string name)
        {
            Transform result = FindChildByName(parent, name);
            if (result == null)
                Debug.LogWarning($"[PortfolioHudPrefabMaterializer] '{name}' was not found. Nothing will be generated for it.");

            return result != null ? result.gameObject : null;
        }

        private static GameObject FindDialoguePanel()
        {
            DialogueManager manager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                Debug.LogWarning("[PortfolioHudPrefabMaterializer] DialogueManager was not found.");
                return null;
            }

            if (manager.dialoguePanel == null)
            {
                Debug.LogWarning("[PortfolioHudPrefabMaterializer] DialogueManager.dialoguePanel is not assigned.");
                return null;
            }

            return manager.dialoguePanel;
        }

        private static void WireHudReferences(
            GameObject hud,
            GameObject header,
            GameObject briefing,
            GameObject players,
            GameObject completion,
            GameObject transition)
        {
            PortfolioDemoHud component = hud.GetComponent<PortfolioDemoHud>();
            if (component == null)
                return;

            SerializedObject serializedHud = new(component);

            Set(serializedHud, "headerCardRoot", GetRect(header));
            Set(serializedHud, "titleText", FindChildText(header, "Title"));
            Set(serializedHud, "controlsText", FindChildText(header, "Controls"));
            Set(serializedHud, "headerEyebrowText", FindChildText(header, "Eyebrow"));
            Set(serializedHud, "statusText", FindChildText(header, "System Status"));

            Set(serializedHud, "demoBriefingRoot", GetRect(briefing));
            Set(serializedHud, "demoBriefingTitleText", FindChildText(briefing, "Current Step"));
            Set(serializedHud, "demoBriefingBodyText", FindChildText(briefing, "Body"));

            Set(serializedHud, "playerPanelRoot", GetRect(players));
            Transform rows = FindChildByName(players != null ? players.transform : null, "Rows");
            Set(serializedHud, "playerListRoot", rows != null ? rows.GetComponent<RectTransform>() : null);
            Set(serializedHud, "playerPanelTitleText", FindChildText(players, "Title"));
            Set(serializedHud, "playerPanelSupportText", FindChildText(players, "Support"));

            Set(serializedHud, "completionPanelRoot", GetRect(completion));
            Set(serializedHud, "completionEyebrowText", FindChildText(completion, "Eyebrow"));
            Set(serializedHud, "completionTitleText", FindChildText(completion, "Title"));
            Set(serializedHud, "completionSupportText", FindChildText(completion, "Support"));
            Set(serializedHud, "completionBodyText", FindChildText(completion, "Body"));
            Set(serializedHud, "completionFooterText", FindChildText(completion, "Footer"));
            Set(serializedHud, "replayButton", FindChildButton(completion, "Replay From Start Button"));
            Set(serializedHud, "closeCompletionButton", FindChildButton(completion, "Close Button"));

            Set(serializedHud, "transitionOverlayRoot", GetRect(transition));
            Set(serializedHud, "transitionOverlayCanvasGroup", transition != null ? transition.GetComponent<CanvasGroup>() : null);
            Set(serializedHud, "transitionOverlayEyebrowText", FindChildText(transition, "Eyebrow"));
            Set(serializedHud, "transitionOverlayTitleText", FindChildText(transition, "Title"));
            Set(serializedHud, "transitionOverlayBodyText", FindChildText(transition, "Body"));

            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireDialogueReferences(GameObject dialoguePanel)
        {
            DialogueManager manager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            if (manager == null || dialoguePanel == null)
                return;

            CanvasGroup dialogueGroup = dialoguePanel.GetComponent<CanvasGroup>();
            SerializedObject serializedManager = new(manager);
            Set(serializedManager, "dialoguePanel", dialoguePanel);
            Set(serializedManager, "dialogueCanvasGroup", dialogueGroup);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            PortfolioDialogueVisualBridge bridge = manager.GetComponent<PortfolioDialogueVisualBridge>();
            if (bridge == null)
                return;

            SerializedObject serializedBridge = new(bridge);
            Set(serializedBridge, "dialogueManager", manager);
            Set(serializedBridge, "subtitleText", FindChildText(dialoguePanel, "Portfolio Subtitle"));
            serializedBridge.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform GetRect(GameObject root)
        {
            return root != null ? root.GetComponent<RectTransform>() : null;
        }

        private static TextMeshProUGUI FindChildText(GameObject root, string name)
        {
            Transform child = FindChildByName(root != null ? root.transform : null, name);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Button FindChildButton(GameObject root, string name)
        {
            Transform child = FindChildByName(root != null ? root.transform : null, name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nested = FindChildByName(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static void ConnectPrefab(GameObject instance, string fileName)
        {
            if (instance == null)
                return;

            string path = $"{PrefabFolder}/{fileName}";
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Set(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }
    }
}
