
    using System;
    using System.IO;
using Unity.Services.Authentication;
using UnityEditor;
    using UnityEngine;
    
    namespace EnglishQuest.Editor
    {
        /// <summary>
        /// Editor debug window for testing the launcher → game authentication handoff.
        ///
        /// Mirrors the launcher's DebugHandoffWindow.  Use it to:
        ///   • Check whether the DPAPI handoff file is on disk.
        ///   • Paste a raw Firebase ID token and simulate what ReadAndDestroy() returns.
        ///   • Drive UnityAuthManager.LoginWithOpenIdConnect() from the Editor.
        ///   • Trigger CloudCodeWhitelistChecker.CheckWhitelistAsync() independently.
        ///   • Inspect and clear the cached PlayerPrefs session.
        ///
        /// Open via  Tools → Auth Debug Window
        /// </summary>
        public class AuthDebugWindow : EditorWindow
        {
            // ─────────────────────────────────────────────────────────────────
            // State
            // ─────────────────────────────────────────────────────────────────
    
            private string _pastedToken        = "";
            private string _manualEmail        = "";
            private string _statusMessage      = "";
            private bool   _statusIsError      = false;
            private bool   _handoffFileExists  = false;
            private string _decodedEmail       = "";
            private Vector2 _scrollPos;
    
            // Colours
            private static readonly Color ColorOk    = new Color(0.31f, 0.78f, 0.47f);
            private static readonly Color ColorError = new Color(1.00f, 0.50f, 0.50f);
            private static readonly Color ColorInfo  = new Color(0.70f, 0.85f, 1.00f);
    
            // ─────────────────────────────────────────────────────────────────
            // Open
            // ─────────────────────────────────────────────────────────────────
    
            [MenuItem("Tools/English Kingdom/Debug/Auth Debug Window")]
            public static void ShowWindow()
            {
                var win = GetWindow<AuthDebugWindow>("Auth Debug");
                win.minSize = new Vector2(480, 560);
                win.RefreshFileStatus();
            }
    
            // ─────────────────────────────────────────────────────────────────
            // GUI
            // ─────────────────────────────────────────────────────────────────
    
            private void OnGUI()
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
    
                DrawSection("Handoff File",     DrawHandoffFileSection);
                DrawSection("Token Input",      DrawTokenInputSection);
                DrawSection("Auth Flow",        DrawAuthFlowSection);
                DrawSection("Whitelist Check",  DrawWhitelistSection);
                DrawSection("Session",          DrawSessionSection);
    
                EditorGUILayout.EndScrollView();
    
                if (!string.IsNullOrEmpty(_statusMessage))
                    DrawStatusBar();
            }
    
            // ─────────────────────────────────────────────────────────────────
            // Sections
            // ─────────────────────────────────────────────────────────────────
    
            private void DrawHandoffFileSection()
            {
                EditorGUILayout.LabelField("Path:", GameHandoffService.HandoffPath, EditorStyles.wordWrappedMiniLabel);
    
                EditorGUILayout.BeginHorizontal();
                RefreshFileStatus();
                Color prev = GUI.color;
                GUI.color = _handoffFileExists ? ColorOk : ColorError;
                EditorGUILayout.LabelField(_handoffFileExists ? "File EXISTS on disk" : "No handoff file on disk",
                    EditorStyles.boldLabel, GUILayout.Height(18));
                GUI.color = prev;
                EditorGUILayout.EndHorizontal();
    
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Read & Destroy (simulate game boot)", GUILayout.Height(24)))
                    SimulateReadAndDestroy();
    
                if (_handoffFileExists && GUILayout.Button("Delete file", GUILayout.Width(90), GUILayout.Height(24)))
                    DeleteHandoffFile();
                EditorGUILayout.EndHorizontal();
            }
    
            private void DrawTokenInputSection()
            {
                EditorGUILayout.LabelField("Paste a Firebase ID token to use instead of the DPAPI file:");
                _pastedToken = EditorGUILayout.TextArea(_pastedToken, GUILayout.Height(60));
    
                if (!string.IsNullOrEmpty(_pastedToken))
                {
                    _decodedEmail = GameBootstrap.DecodeEmailFromJwt(_pastedToken);
                    EditorGUILayout.LabelField("Decoded email:", _decodedEmail, EditorStyles.miniLabel);
                }
    
                if (GUILayout.Button("Clear token", GUILayout.Height(20)))
                {
                    _pastedToken   = "";
                    _decodedEmail  = "";
                }
            }
    
            private void DrawAuthFlowSection()
            {
                EditorGUILayout.HelpBox("UNITY_AUTHENTICATION_ENABLED is defined — live calls are active.", MessageType.Info);
    
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to run authentication calls.", MessageType.Warning);
                    return;
                }
    
                if (GUILayout.Button("Login with pasted token (OIDC)", GUILayout.Height(28)))
                    LoginWithPastedToken();
    
                if (GUILayout.Button("Simulate full boot (ReadAndDestroy → LoginWithOpenIdConnect)", GUILayout.Height(28)))
                    SimulateFullBoot();
            }
    
            private void DrawWhitelistSection()
            {
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to run Cloud Code calls.", MessageType.Warning);
                    return;
                }
    
                string tokenPreview = string.IsNullOrEmpty(_pastedToken) ? "(none — paste a token above)" : _pastedToken[..Math.Min(40, _pastedToken.Length)] + "...";
                EditorGUILayout.LabelField("Token to send:", tokenPreview, EditorStyles.miniLabel);
    
                if (GUILayout.Button("Check whitelist via Cloud Code", GUILayout.Height(28)))
                    CheckWhitelistManually();
            }
    
            private void DrawSessionSection()
            {
                string cachedEmail = PlayerPrefs.GetString("playerEmail", "(none)");
                EditorGUILayout.LabelField("Cached email (PlayerPrefs):", cachedEmail);
    
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear PlayerPrefs session", GUILayout.Height(22)))
                {
                    PlayerPrefs.DeleteKey("playerEmail");
                    PlayerPrefs.Save();
                    SetStatus("PlayerPrefs session cleared.", isError: false);
                }
    
                if (Application.isPlaying && GUILayout.Button("Sign Out (Unity Auth)", GUILayout.Height(22)))
                {
                AuthenticationService.Instance.SignOut();
                    SetStatus("Signed out from Unity Authentication.", isError: false);
                }
                EditorGUILayout.EndHorizontal();
            }
    
            // ─────────────────────────────────────────────────────────────────
            // Actions
            // ─────────────────────────────────────────────────────────────────
    
            private void SimulateReadAndDestroy()
            {
                string token = GameHandoffService.ReadAndDestroy();
                RefreshFileStatus();
    
                if (string.IsNullOrEmpty(token))
                {
                    SetStatus("ReadAndDestroy returned null — file missing or decryption failed.", isError: true);
                    return;
                }
    
                _pastedToken  = token;
                _decodedEmail = GameBootstrap.DecodeEmailFromJwt(token);
                SetStatus($"Token read successfully. Email: {_decodedEmail}", isError: false);
                Repaint();
            }
    
            private void DeleteHandoffFile()
            {
                try
                {
                    File.Delete(GameHandoffService.HandoffPath);
                    RefreshFileStatus();
                    SetStatus("Handoff file deleted.", isError: false);
                }
                catch (Exception ex)
                {
                    SetStatus($"Delete failed: {ex.Message}", isError: true);
                }
            }
    
            private void LoginWithPastedToken()
            {
                if (string.IsNullOrEmpty(_pastedToken))
                {
                    SetStatus("No token pasted.", isError: true);
                    return;
                }
    
                UnityAuthManager.OnLoginSuccess += OnAuthSuccess;
                UnityAuthManager.OnLoginFailed  += OnAuthFailed;
                UnityAuthManager.Instance.LoginWithOpenIdConnect(_pastedToken);
                SetStatus("OIDC sign-in call fired — waiting for response...", isError: false);
            }
    
            private void SimulateFullBoot()
            {
                string token = GameHandoffService.ReadAndDestroy();
                RefreshFileStatus();
    
                if (string.IsNullOrEmpty(token))
                {
                    SetStatus("No handoff file found. Paste a token manually instead.", isError: true);
                    return;
                }
    
                _pastedToken  = token;
                _decodedEmail = GameBootstrap.DecodeEmailFromJwt(token);
    
                UnityAuthManager.OnLoginSuccess += OnAuthSuccess;
                UnityAuthManager.OnLoginFailed  += OnAuthFailed;
                UnityAuthManager.Instance.LoginWithOpenIdConnect(token);
                SetStatus($"Full boot simulated for '{_decodedEmail}' — waiting for auth...", isError: false);
            }
    
            private void CheckWhitelistManually()
            {
                if (string.IsNullOrEmpty(_pastedToken))
                {
                    SetStatus("No token available — paste a Firebase ID token above.", isError: true);
                    return;
                }
    
                CloudCodeWhitelistChecker.OnWhitelistGranted += OnWhitelistGranted;
                CloudCodeWhitelistChecker.OnWhitelistDenied  += OnWhitelistDenied;
    
                // Find or create a temporary checker in the scene
                var checker = FindFirstObjectByType<CloudCodeWhitelistChecker>()
                           ?? new GameObject("[AuthDebug] WhitelistChecker").AddComponent<CloudCodeWhitelistChecker>();
    
                _ = checker.CheckWhitelistAsync(_pastedToken);
                SetStatus("Whitelist check fired with pasted token...", isError: false);
            }
    
            // ── Auth callbacks ──────────────────────────────────────────────
    
            private void OnAuthSuccess(string email)
            {
                UnityAuthManager.OnLoginSuccess -= OnAuthSuccess;
                UnityAuthManager.OnLoginFailed  -= OnAuthFailed;
                SetStatus($"Auth success for '{email}'.", isError: false);
                Repaint();
            }
    
            private void OnAuthFailed(string error)
            {
                UnityAuthManager.OnLoginSuccess -= OnAuthSuccess;
                UnityAuthManager.OnLoginFailed  -= OnAuthFailed;
                SetStatus($"Auth failed: {error}", isError: true);
                Repaint();
            }
    
            private void OnWhitelistGranted(string email)
            {
                CloudCodeWhitelistChecker.OnWhitelistGranted -= OnWhitelistGranted;
                CloudCodeWhitelistChecker.OnWhitelistDenied  -= OnWhitelistDenied;
                SetStatus($"Whitelist GRANTED for '{email}'.", isError: false);
                Repaint();
            }
    
            private void OnWhitelistDenied(string reason)
            {
                CloudCodeWhitelistChecker.OnWhitelistGranted -= OnWhitelistGranted;
                CloudCodeWhitelistChecker.OnWhitelistDenied  -= OnWhitelistDenied;
                SetStatus($"Whitelist DENIED: {reason}", isError: true);
                Repaint();
            }
    
            // ─────────────────────────────────────────────────────────────────
            // Helpers
            // ─────────────────────────────────────────────────────────────────
    
            private void RefreshFileStatus()
            {
                _handoffFileExists = File.Exists(GameHandoffService.HandoffPath);
            }
    
            private void SetStatus(string message, bool isError)
            {
                _statusMessage = message;
                _statusIsError = isError;
                AppLog.Info($"[AuthDebug] {message}");
                Repaint();
            }
    
            private void DrawStatusBar()
            {
                Color prev = GUI.color;
                GUI.color = _statusIsError ? ColorError : ColorOk;
                EditorGUILayout.HelpBox(_statusMessage, _statusIsError ? MessageType.Error : MessageType.Info);
                GUI.color = prev;
            }
    
            private static void DrawSection(string title, Action content)
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                content();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }
        }
    }
    

