using EnglishQuest.PortfolioDemo;
using System.Collections.Generic;
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
            if (ReferenceEquals(hud, null))
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
            if (ReferenceEquals(result, null))
                Debug.LogWarning($"[PortfolioHudPrefabMaterializer] '{name}' was not found. Nothing will be generated for it.");

            return ReferenceEquals(result, null) ? null : result.gameObject;
        }

        private static GameObject FindDialoguePanel()
        {
            DialogueManager manager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            if (ReferenceEquals(manager, null))
            {
                Debug.LogWarning("[PortfolioHudPrefabMaterializer] DialogueManager was not found.");
                return null;
            }

            if (ReferenceEquals(manager.dialoguePanel, null))
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
            if (ReferenceEquals(component, null))
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
            Transform rows = FindChildByName(ReferenceEquals(players, null) ? null : players.transform, "Rows");
            Set(serializedHud, "playerListRoot", ReferenceEquals(rows, null) ? null : rows.GetComponent<RectTransform>());
            Set(serializedHud, "playerPanelTitleText", FindChildText(players, "Title"));
            Set(serializedHud, "playerPanelSupportText", FindChildText(players, "Support"));

            Set(serializedHud, "completionPanelRoot", GetRect(completion));
            Set(serializedHud, "completionEyebrowText", FindChildText(completion, "Eyebrow") ?? FindChildText(completion, "Section Label"));
            Set(serializedHud, "completionTitleText", FindChildText(completion, "Title"));
            Set(serializedHud, "completionSupportText", FindChildText(completion, "Support") ?? FindChildText(completion, "Current Step"));
            Set(serializedHud, "completionBodyText", FindChildText(completion, "Body"));
            Set(serializedHud, "completionFooterText", FindChildText(completion, "Footer"));
            Set(serializedHud, "replayButton", FindChildButton(completion, "Replay From Start Button") ?? FindChildButton(completion, "Start Again Button"));
            Set(serializedHud, "closeCompletionButton", FindChildButton(completion, "Close Button") ?? FindChildButton(completion, "Continue Button"));

            Set(serializedHud, "transitionOverlayRoot", GetRect(transition));
            Set(serializedHud, "transitionOverlayCanvasGroup", ReferenceEquals(transition, null) ? null : transition.GetComponent<CanvasGroup>());
            Set(serializedHud, "transitionOverlayEyebrowText", FindChildText(transition, "Eyebrow"));
            Set(serializedHud, "transitionOverlayTitleText", FindChildText(transition, "Title"));
            Set(serializedHud, "transitionOverlayBodyText", FindChildText(transition, "Body"));

            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireDialogueReferences(GameObject dialoguePanel)
        {
            DialogueManager manager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            if (ReferenceEquals(manager, null) || ReferenceEquals(dialoguePanel, null))
                return;

            CanvasGroup dialogueGroup = dialoguePanel.GetComponent<CanvasGroup>();
            SerializedObject serializedManager = new(manager);
            Set(serializedManager, "dialoguePanel", dialoguePanel);
            Set(serializedManager, "dialogueCanvasGroup", dialogueGroup);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            PortfolioDialogueVisualBridge bridge = manager.GetComponent<PortfolioDialogueVisualBridge>();
            if (ReferenceEquals(bridge, null))
                return;

            SerializedObject serializedBridge = new(bridge);
            Set(serializedBridge, "dialogueManager", manager);
            Set(serializedBridge, "subtitleText", FindChildText(dialoguePanel, "Portfolio Subtitle"));
            serializedBridge.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform GetRect(GameObject root)
        {
            return ReferenceEquals(root, null) ? null : root.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI FindChildText(GameObject root, string name)
        {
            Transform child = FindChildByName(ReferenceEquals(root, null) ? null : root.transform, name);
            return ReferenceEquals(child, null) ? null : child.GetComponent<TextMeshProUGUI>();
        }

        private static Button FindChildButton(GameObject root, string name)
        {
            Transform child = FindChildByName(ReferenceEquals(root, null) ? null : root.transform, name);
            return ReferenceEquals(child, null) ? null : child.GetComponent<Button>();
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (ReferenceEquals(root, null) || string.IsNullOrEmpty(childName))
                return null;

            HashSet<int> visited = new();
            Stack<Transform> pending = new();
            pending.Push(root);

            while (pending.Count > 0)
            {
                Transform current = pending.Pop();
                if (ReferenceEquals(current, null))
                    continue;

                int id = current.GetInstanceID();
                if (!visited.Add(id))
                    continue;

                for (int i = current.childCount - 1; i >= 0; i--)
                {
                    Transform child = current.GetChild(i);
                    if (ReferenceEquals(child, null))
                        continue;

                    if (child.name == childName)
                        return child;

                    pending.Push(child);
                }
            }

            return null;
        }

        private static void ConnectPrefab(GameObject instance, string fileName)
        {
            if (ReferenceEquals(instance, null))
                return;

            string path = $"{PrefabFolder}/{fileName}";
            PrefabUtility.SaveAsPrefabAsset(instance, path);
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
            if (!ReferenceEquals(property, null))
                property.objectReferenceValue = value;
        }
    }
}
