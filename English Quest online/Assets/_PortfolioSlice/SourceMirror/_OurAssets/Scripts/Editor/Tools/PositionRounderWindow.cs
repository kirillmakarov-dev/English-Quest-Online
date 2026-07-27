using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace EnglishQuest.Editor.Tools
{
    /// <summary>
    /// Unity Editor window tool for rounding object positions to specified margins.
    /// Follows Clean Architecture principles with single responsibility.
    /// </summary>
    public class PositionRounderWindow : EditorWindow
    {
        #region Fields

        private enum RoundingMode { TwoDecimals, NearestHalf }

        private bool _roundX = true;
        private bool _roundY = true;
        private bool _roundZ = true;
        private bool _includeChildren = false;
        private RoundingMode _roundingMode = RoundingMode.TwoDecimals;
        private Vector2 _scrollPosition;
        
        #endregion
        
        #region Unity Editor Menu
        
        [MenuItem("Tools/English Kingdom/Scene/Position Rounder")]
        public static void ShowWindow()
        {
            var window = GetWindow<PositionRounderWindow>("Position Rounder");
            window.minSize = new Vector2(300, 200);
            window.Show();
        }
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            DrawPreview();
            DrawActionButtons();
        }
        
        #endregion
        
        #region GUI Drawing Methods
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Position Rounder Tool", headerStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "This tool rounds the local positions of selected GameObjects. " +
                "Choose between 2 decimal places or nearest 0.5 increment. " +
                "Useful for cleaning up imprecise positions and removing floating point errors.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();

            // Rounding mode
            EditorGUILayout.LabelField("Rounding Mode:", EditorStyles.miniBoldLabel);
            _roundingMode = (RoundingMode)EditorGUILayout.EnumPopup(_roundingMode);
            EditorGUILayout.Space(5);

            // Axis selection
            string modeLabel = _roundingMode == RoundingMode.TwoDecimals ? "Axes to Round (to 2 decimal places):" : "Axes to Round (to nearest 0.5):";
            EditorGUILayout.LabelField(modeLabel, EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _roundX = EditorGUILayout.ToggleLeft("X", _roundX, GUILayout.Width(50));
            _roundY = EditorGUILayout.ToggleLeft("Y", _roundY, GUILayout.Width(50));
            _roundZ = EditorGUILayout.ToggleLeft("Z", _roundZ, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Include children option
            _includeChildren = EditorGUILayout.ToggleLeft(
                new GUIContent("Include Children", "Also round positions of all child objects recursively"),
                _includeChildren
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawPreview()
        {
            var targetObjects = GetTargetObjects();
            
            if (targetObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("Select GameObjects in the scene to preview position changes.", MessageType.Warning);
                return;
            }
            
            string childrenText = _includeChildren ? " (incl. children)" : "";
            EditorGUILayout.LabelField($"Preview ({targetObjects.Count} objects{childrenText})", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(150));
            
            foreach (var obj in targetObjects.Take(10)) // Limit preview to first 10 objects
            {
                if (obj == null) continue;
                
                var currentPos = obj.transform.localPosition;
                var roundedPos = RoundPosition(currentPos);
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField(obj.name, GUILayout.Width(120));
                
                // Current position (abbreviated)
                EditorGUILayout.LabelField($"({currentPos.x:F2}, {currentPos.y:F2}, {currentPos.z:F2})", GUILayout.Width(100));
                
                EditorGUILayout.LabelField("→", GUILayout.Width(20));
                
                // Rounded position (abbreviated)
                EditorGUILayout.LabelField($"({roundedPos.x:F2}, {roundedPos.y:F2}, {roundedPos.z:F2})", GUILayout.Width(100));
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (targetObjects.Count > 10)
            {
                EditorGUILayout.LabelField($"... and {targetObjects.Count - 10} more objects", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);
        }
        
        private void DrawActionButtons()
        {
            var targetObjects = GetTargetObjects();
            bool hasSelection = targetObjects.Count > 0;
            
            EditorGUI.BeginDisabledGroup(!hasSelection);
            
            EditorGUILayout.BeginHorizontal();
            
            // Apply button
            if (GUILayout.Button($"Round Positions ({targetObjects.Count} objects)", GUILayout.Height(30)))
            {
                ApplyPositionRounding();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            
            if (!hasSelection)
            {
                EditorGUILayout.HelpBox("Select one or more GameObjects to enable position rounding.", MessageType.Info);
            }
        }
        
        #endregion
        
        #region Core Logic
        
        /// <summary>
        /// Rounds a position vector to 2 decimal places based on current settings.
        /// </summary>
        private Vector3 RoundPosition(Vector3 position)
        {
            return new Vector3(
                _roundX ? RoundValue(position.x) : position.x,
                _roundY ? RoundValue(position.y) : position.y,
                _roundZ ? RoundValue(position.z) : position.z
            );
        }
        
        /// <summary>
        /// Rounds a float value according to the current rounding mode.
        /// </summary>
        private float RoundValue(float value)
        {
            return _roundingMode == RoundingMode.NearestHalf
                ? RoundToNearestHalf(value)
                : RoundToTwoDecimals(value);
        }

        /// <summary>
        /// Rounds a float value to 2 decimal places.
        /// </summary>
        private float RoundToTwoDecimals(float value)
        {
            return (float)System.Math.Round(value, 2, System.MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rounds a float value to the nearest 0.5 increment.
        /// </summary>
        private float RoundToNearestHalf(float value)
        {
            return Mathf.Round(value * 2f) / 2f;
        }
        
        /// <summary>
        /// Gets all target GameObjects based on selection and include children setting.
        /// </summary>
        private List<GameObject> GetTargetObjects()
        {
            var result = new List<GameObject>();
            var selectedObjects = Selection.gameObjects;
            
            foreach (var obj in selectedObjects)
            {
                if (obj == null) continue;
                
                // Add the selected object itself
                if (!result.Contains(obj))
                {
                    result.Add(obj);
                }
                
                // Add children if enabled
                if (_includeChildren)
                {
                    var children = obj.GetComponentsInChildren<Transform>(true);
                    foreach (var child in children)
                    {
                        if (child.gameObject != obj && !result.Contains(child.gameObject))
                        {
                            result.Add(child.gameObject);
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Applies position rounding to all selected GameObjects.
        /// Uses SerializedObject for proper prefab mode support.
        /// </summary>
        private void ApplyPositionRounding()
        {
            var targetObjects = GetTargetObjects();
            
            if (targetObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects to round positions.", "OK");
                return;
            }
            
            // Check if we're in Prefab Mode
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            bool isInPrefabMode = prefabStage != null;
            
            int processedCount = 0;
            
            foreach (var obj in targetObjects)
            {
                if (obj == null) continue;
                
                var transform = obj.transform;
                var originalPosition = transform.localPosition;
                var roundedPosition = RoundPosition(originalPosition);
                
                // Debug log to see values
                AppLog.Info($"[Position Rounder] {obj.name}: Original={originalPosition} -> Rounded={roundedPosition}");
                
                // Use SerializedObject for proper prefab/undo support
                SerializedObject serializedTransform = new SerializedObject(transform);
                SerializedProperty localPositionProp = serializedTransform.FindProperty("m_LocalPosition");
                
                if (localPositionProp != null)
                {
                    // Get current values
                    Vector3 currentValue = localPositionProp.vector3Value;
                    Vector3 newValue = new Vector3(
                        _roundX ? RoundValue(currentValue.x) : currentValue.x,
                        _roundY ? RoundValue(currentValue.y) : currentValue.y,
                        _roundZ ? RoundValue(currentValue.z) : currentValue.z
                    );
                    
                    // Apply new value
                    localPositionProp.vector3Value = newValue;
                    
                    // Apply and record undo
                    if (serializedTransform.ApplyModifiedProperties())
                    {
                        processedCount++;
                        AppLog.Info($"[Position Rounder] Applied: {obj.name} = {newValue}");
                    }
                }
            }
            
            // Mark appropriate context as dirty
            if (processedCount > 0)
            {
                if (isInPrefabMode)
                {
                    EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                }
                else
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
            
            AppLog.Info($"Position Rounder: Successfully rounded positions for {processedCount} out of {targetObjects.Count} selected objects." + 
                      (isInPrefabMode ? " (Prefab Mode)" : "") +
                      (_includeChildren ? " (Including children)" : ""));
            
            Repaint();
        }
        
        #endregion
    }
}
