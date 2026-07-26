using System.IO;
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools
{
    /// <summary>
    /// Editor window for configuring and triggering the Build → Deploy pipeline.
    /// Open via  Tools → English Kingdom → Build → Deploy Settings
    /// </summary>
    public class BuildDeployWindow : EditorWindow
    {
        // ─── State (mirrors EditorPrefs; refreshed on focus) ─────────────────
        private bool   _uploadOnFinish;
        private string _deployScriptPath;
        private string _gameOutputFolder;
        private string _statusMessage;
        private bool   _statusIsError;

        // ─── Colours ─────────────────────────────────────────────────────────
        private static readonly Color ColorOk    = new Color(0.31f, 0.78f, 0.47f);
        private static readonly Color ColorError = new Color(1.00f, 0.50f, 0.50f);

        // ─── Open ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/English Kingdom/Build/Deploy Settings")]
        public static void ShowWindow()
        {
            var win = GetWindow<BuildDeployWindow>("Build Deploy");
            win.minSize = new Vector2(480, 260);
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void OnFocus()
        {
            // Pull fresh values whenever the window gains focus (handles external
            // changes or domain reloads).
            _uploadOnFinish   = BuildDeploySettings.UploadOnFinish;
            _deployScriptPath = BuildDeploySettings.DeployScriptPath;
            _gameOutputFolder = BuildDeploySettings.GameOutputFolder;
        }

        // ─── GUI ──────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6);
            DrawToggle();
            EditorGUILayout.Space(4);
            DrawPathField("Deploy Script (.ps1)", ref _deployScriptPath,
                          changed => BuildDeploySettings.DeployScriptPath = changed,
                          isFolder: false);
            DrawPathField("Game Output Folder", ref _gameOutputFolder,
                          changed => BuildDeploySettings.GameOutputFolder = changed,
                          isFolder: true);
            EditorGUILayout.Space(10);
            DrawDeployNowButton();
            DrawStatus();
        }

        // ─── Draw helpers ─────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUIStyle header = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 15,
                alignment = TextAnchor.MiddleCenter,
            };
            EditorGUILayout.LabelField("Build → Deploy Settings", header);
            DrawSeparator();
        }

        private void DrawToggle()
        {
            EditorGUI.BeginChangeCheck();
            bool newVal = EditorGUILayout.ToggleLeft(
                new GUIContent("Upload on Finish",
                    "When enabled, Deploy.ps1 is launched automatically after each successful build."),
                _uploadOnFinish,
                EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
            {
                _uploadOnFinish = newVal;
                BuildDeploySettings.UploadOnFinish = newVal;
            }
        }

        private void DrawPathField(string label, ref string value,
                                   System.Action<string> onChanged, bool isFolder)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                string newVal = EditorGUILayout.TextField(value);
                if (EditorGUI.EndChangeCheck())
                {
                    value = newVal;
                    onChanged(newVal);
                }

                if (GUILayout.Button("Browse…", GUILayout.Width(68)))
                {
                    string picked = isFolder
                        ? EditorUtility.OpenFolderPanel(label, value, "")
                        : EditorUtility.OpenFilePanel(label, Path.GetDirectoryName(value),
                                                      "ps1");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        // OpenFolderPanel/OpenFilePanel returns forward-slashes; normalise.
                        picked = picked.Replace('/', '\\');
                        value  = picked;
                        onChanged(picked);
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        private void DrawDeployNowButton()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(0.25f, 0.60f, 1.00f);
                if (GUILayout.Button("Deploy Now", GUILayout.Width(140), GUILayout.Height(28)))
                {
                    TriggerDeploy();
                }
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_statusMessage)) return;
            EditorGUILayout.Space(6);
            GUI.color = _statusIsError ? ColorError : ColorOk;
            EditorGUILayout.HelpBox(_statusMessage,
                _statusIsError ? MessageType.Error : MessageType.Info);
            GUI.color = Color.white;
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.35f, 0.35f, 0.35f));
            EditorGUILayout.Space(4);
        }

        // ─── Deploy trigger ───────────────────────────────────────────────────
        private void TriggerDeploy()
        {
            if (!File.Exists(_deployScriptPath))
            {
                SetStatus($"Deploy script not found:\n{_deployScriptPath}", isError: true);
                return;
            }

            if (!Directory.Exists(_gameOutputFolder))
            {
                SetStatus($"Game output folder not found:\n{_gameOutputFolder}", isError: true);
                return;
            }

            DeployRunner.Run(_deployScriptPath, _gameOutputFolder);
            SetStatus("PowerShell deploy window launched.", isError: false);
        }

        internal static void TriggerDeployFromPostprocessor(string buildOutputFolder = null)
        {
            // Called by the postprocessor — validates and runs without a window reference.
            string script = BuildDeploySettings.DeployScriptPath;
            string folder = string.IsNullOrEmpty(buildOutputFolder)
                ? BuildDeploySettings.GameOutputFolder
                : buildOutputFolder;

            if (!File.Exists(script))
            {
                Debug.LogError($"[BuildDeploy] Deploy script not found: {script}");
                return;
            }

            if (!Directory.Exists(folder))
            {
                Debug.LogError($"[BuildDeploy] Game output folder not found: {folder}");
                return;
            }

            DeployRunner.Run(script, folder);
        }

        private void SetStatus(string message, bool isError)
        {
            _statusMessage = message;
            _statusIsError = isError;
            Repaint();
        }
    }
}
