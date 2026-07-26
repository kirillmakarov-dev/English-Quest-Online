using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace EnglishKingdom.SaveSystem.Editor
{
    public sealed class SaveDataEditorWindow : EditorWindow
    {
        private const float TextAreaMinHeight = 320f;
        private static readonly Color LoadColor = new Color(0.55f, 0.75f, 1f);
        private static readonly Color SaveColor = new Color(0.55f, 0.9f, 0.55f);
        private static readonly Color DeleteColor = new Color(1f, 0.55f, 0.55f);
        private static readonly Color NeutralColor = Color.white;

        private int _selectedDomainIndex;
        private string _localJson = string.Empty;
        private string _cloudJson = string.Empty;
        private Vector2 _localScroll;
        private Vector2 _cloudScroll;
        private bool _isBusy;
        private string _lastStatus = "Ready.";
        private MessageType _lastStatusType = MessageType.Info;
        private GUIStyle _jsonStyle;
        private GUIStyle _panelHeaderStyle;

        [MenuItem("Tools/Save Data Inspector")]
        private static void Open()
        {
            var window = GetWindow<SaveDataEditorWindow>();
            window.titleContent = new GUIContent("Save Inspector");
            window.minSize = new Vector2(960f, 560f);
            window.Show();
        }

        private SaveEditorDomainRegistry.DomainDescriptor SelectedDomain =>
            SaveEditorDomainRegistry.All[Mathf.Clamp(_selectedDomainIndex, 0, SaveEditorDomainRegistry.All.Count - 1)];

        private void OnGUI()
        {
            EnsureStyles();

            DrawTitleBar();
            DrawPlayModeGuard();

            using (new EditorGUI.DisabledScope(!Application.isPlaying || _isBusy))
            {
                DrawDomainBar();
                EditorGUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();
                DrawBackendPanel(
                    "Local  (Application.persistentDataPath)",
                    ref _localJson,
                    ref _localScroll,
                    () => LoadLocalAsync(),
                    () => SaveLocalAsync(),
                    () => DeleteLocalAsync(),
                    () => _localJson = SaveEditorBackendBridge.BuildDefaultWrapperJson(SelectedDomain));

                GUILayout.Space(10f);

                DrawBackendPanel(
                    "Cloud  (Unity Cloud Save)",
                    ref _cloudJson,
                    ref _cloudScroll,
                    () => LoadCloudAsync(),
                    () => SaveCloudAsync(),
                    () => DeleteCloudAsync(),
                    () => _cloudJson = SaveEditorBackendBridge.BuildDefaultWrapperJson(SelectedDomain));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(8f);
                DrawFooterBar();
            }

            DrawStatusBar();
        }

        private void EnsureStyles()
        {
            if (_jsonStyle == null)
            {
                _jsonStyle = new GUIStyle(EditorStyles.textArea)
                {
                    font = EditorStyles.miniLabel.font,
                    wordWrap = false,
                    fontSize = 11
                };
            }

            if (_panelHeaderStyle == null)
            {
                _panelHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };
            }
        }

        private void DrawTitleBar()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Save Data Inspector", new GUIStyle(EditorStyles.largeLabel) { fontSize = 16, fontStyle = FontStyle.Bold });
            EditorGUILayout.LabelField(
                "Inspect, edit and delete raw save JSON on Local disk and Unity Cloud Save — live, while the game is running.",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);
        }

        private void DrawPlayModeGuard()
        {
            if (Application.isPlaying)
                return;

            EditorGUILayout.HelpBox(
                "▶ Enter Play Mode to enable Load / Save / Delete actions.",
                MessageType.Warning);
            EditorGUILayout.Space(4f);
        }

        private void DrawDomainBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            string[] names = new string[SaveEditorDomainRegistry.All.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = SaveEditorDomainRegistry.All[i].DisplayName;

            EditorGUILayout.LabelField("Save Domain", GUILayout.Width(80f));
            int updated = EditorGUILayout.Popup(_selectedDomainIndex, names, GUILayout.Width(160f));
            if (updated != _selectedDomainIndex)
            {
                _selectedDomainIndex = updated;
                SetStatus($"Selected domain: {SelectedDomain.DisplayName} ({SelectedDomain.Key})", MessageType.Info);
            }

            GUILayout.Space(20f);
            EditorGUILayout.LabelField("Key:", GUILayout.Width(28f));
            EditorGUILayout.SelectableLabel(SelectedDomain.Key, EditorStyles.miniBoldLabel, GUILayout.Width(140f), GUILayout.Height(16f));

            GUILayout.Space(20f);
            EditorGUILayout.LabelField("Latest Version:", GUILayout.Width(90f));
            EditorGUILayout.LabelField(SelectedDomain.LatestVersion.ToString(), EditorStyles.miniBoldLabel, GUILayout.Width(24f));

            GUILayout.FlexibleSpace();

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = NeutralColor;
            if (GUILayout.Button("⟳ Reload Both", GUILayout.Width(120f)))
                RunOperation(() => ReloadBothAsync());
            GUI.backgroundColor = prevColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawBackendPanel(
            string title,
            ref string json,
            ref Vector2 scroll,
            Func<UniTask> onLoad,
            Func<UniTask> onSave,
            Func<UniTask> onDelete,
            Action onDefault)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField(title, _panelHeaderStyle);
            EditorGUILayout.Space(4f);

            Color prevColor = GUI.backgroundColor;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = LoadColor;
            if (GUILayout.Button("▼ Load", GUILayout.Height(24f)))
                RunOperation(onLoad);

            GUI.backgroundColor = SaveColor;
            if (GUILayout.Button("▲ Save", GUILayout.Height(24f)))
                RunOperation(onSave);

            GUI.backgroundColor = DeleteColor;
            if (GUILayout.Button("✕ Delete", GUILayout.Height(24f)))
                RunOperation(onDelete);

            GUI.backgroundColor = prevColor;
            if (GUILayout.Button("Fill Default", GUILayout.Height(24f), GUILayout.Width(90f)))
                onDefault();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            var localJson = json;
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(TextAreaMinHeight));
            localJson = EditorGUILayout.TextArea(localJson, _jsonStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            json = localJson;

            EditorGUILayout.EndVertical();
        }

        private void DrawFooterBar()
        {
            EditorGUILayout.LabelField(
                "Tip: payloads follow the VersionedWrapper<T> shape — { \"version\": .., \"savedAt\": .., \"data\": { .. } }. " +
                "\"version\" and \"savedAt\" are auto-filled on save if omitted.",
                EditorStyles.miniLabel);
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox((_isBusy ? "⏳ " : string.Empty) + _lastStatus, _lastStatusType);
        }

        private async UniTask LoadLocalAsync()
        {
            string json = await SaveEditorBackendBridge.LoadLocalRawJsonAsync(SelectedDomain);
            _localJson = json ?? string.Empty;
            SetStatus(json == null
                ? $"No local data found for '{SelectedDomain.Key}'."
                : $"Loaded local data for '{SelectedDomain.Key}'.", MessageType.Info);
        }

        private async UniTask SaveLocalAsync()
        {
            await SaveEditorBackendBridge.SaveLocalRawJsonAsync(SelectedDomain, _localJson);
            await LoadLocalAsync();
            SetStatus($"Saved local data for '{SelectedDomain.Key}'.", MessageType.Info);
        }

        private async UniTask DeleteLocalAsync()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Local Save",
                $"Delete local save data for key '{SelectedDomain.Key}'?",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            await SaveEditorBackendBridge.DeleteLocalAsync(SelectedDomain);
            _localJson = string.Empty;
            SetStatus($"Deleted local data for '{SelectedDomain.Key}'.", MessageType.Info);
        }

        private async UniTask LoadCloudAsync()
        {
            string json = await SaveEditorBackendBridge.LoadCloudRawJsonAsync(SelectedDomain);
            _cloudJson = json ?? string.Empty;
            SetStatus(json == null
                ? $"No cloud data found for '{SelectedDomain.Key}'."
                : $"Loaded cloud data for '{SelectedDomain.Key}'.", MessageType.Info);
        }

        private async UniTask SaveCloudAsync()
        {
            await SaveEditorBackendBridge.SaveCloudRawJsonAsync(SelectedDomain, _cloudJson);
            await LoadCloudAsync();
            SetStatus($"Saved cloud data for '{SelectedDomain.Key}'.", MessageType.Info);
        }

        private async UniTask DeleteCloudAsync()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Cloud Save",
                $"Delete cloud save data for key '{SelectedDomain.Key}'?",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            await SaveEditorBackendBridge.DeleteCloudAsync(SelectedDomain);
            _cloudJson = string.Empty;
            SetStatus($"Deleted cloud data for '{SelectedDomain.Key}'.", MessageType.Info);
        }

        private async UniTask ReloadBothAsync()
        {
            await LoadLocalAsync();
            await LoadCloudAsync();
            SetStatus($"Reloaded local and cloud payloads for '{SelectedDomain.Key}'.", MessageType.Info);
        }

        private void RunOperation(Func<UniTask> operation)
        {
            RunOperationAsync(operation).Forget();
        }

        private async UniTaskVoid RunOperationAsync(Func<UniTask> operation)
        {
            if (_isBusy)
                return;

            _isBusy = true;
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, MessageType.Error);
                AppLog.Error($"[SaveDataEditorWindow] {ex}");
            }
            finally
            {
                _isBusy = false;
                Repaint();
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            _lastStatus = message;
            _lastStatusType = type;
            Repaint();
        }
    }
}
#endif
