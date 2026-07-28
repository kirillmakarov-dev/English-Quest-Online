using UnityEditor;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo.Editor
{
    [CustomEditor(typeof(PortfolioQuestProgressController))]
    public sealed class PortfolioQuestProgressControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _applySelectionOnPlay;
        private SerializedProperty _clearSavedProgressBeforeApply;
        private SerializedProperty _applyMode;
        private SerializedProperty _selectedQuestIndex;
        private SerializedProperty _selectedStepIndex;
        private SerializedProperty _completePreviousQuests;
        private SerializedProperty _forceStartSelectedQuest;
        private SerializedProperty _logQuestStatesAfterApply;
        private SerializedProperty _questManager;

        private void OnEnable()
        {
            _applySelectionOnPlay = serializedObject.FindProperty("applySelectionOnPlay");
            _clearSavedProgressBeforeApply = serializedObject.FindProperty("clearSavedProgressBeforeApply");
            _applyMode = serializedObject.FindProperty("applyMode");
            _selectedQuestIndex = serializedObject.FindProperty("selectedQuestIndex");
            _selectedStepIndex = serializedObject.FindProperty("selectedStepIndex");
            _completePreviousQuests = serializedObject.FindProperty("completePreviousQuests");
            _forceStartSelectedQuest = serializedObject.FindProperty("forceStartSelectedQuest");
            _logQuestStatesAfterApply = serializedObject.FindProperty("logQuestStatesAfterApply");
            _questManager = serializedObject.FindProperty("questManager");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Auto Apply", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_applySelectionOnPlay);
            EditorGUILayout.PropertyField(_clearSavedProgressBeforeApply);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_applyMode);

            DrawQuestSelection();

            if ((PortfolioQuestProgressController.ApplyMode)_applyMode.enumValueIndex ==
                PortfolioQuestProgressController.ApplyMode.StartFromQuest)
            {
                EditorGUILayout.PropertyField(_completePreviousQuests);
                EditorGUILayout.PropertyField(_forceStartSelectedQuest);
            }

            EditorGUILayout.PropertyField(_logQuestStatesAfterApply);

            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(_questManager);

            serializedObject.ApplyModifiedProperties();

            DrawActions();
        }

        private void DrawQuestSelection()
        {
            var controller = (PortfolioQuestProgressController)target;
            var mode = (PortfolioQuestProgressController.ApplyMode)_applyMode.enumValueIndex;
            if (mode != PortfolioQuestProgressController.ApplyMode.StartFromQuest)
                return;

            if (Application.isPlaying)
            {
                var quests = controller.GetOrderedQuests();
                if (quests.Count > 0)
                {
                    string[] labels = new string[quests.Count];
                    for (int i = 0; i < quests.Count; i++)
                    {
                        QuestInfo quest = quests[i];
                        string questName = quest != null
                            ? $"{i}: {quest.displayName} ({quest.id})"
                            : $"{i}: <null>";
                        labels[i] = questName;
                    }

                    int clampedQuestIndex = Mathf.Clamp(_selectedQuestIndex.intValue, 0, labels.Length - 1);
                    int selectedQuest = EditorGUILayout.Popup("Selected Quest", clampedQuestIndex, labels);
                    _selectedQuestIndex.intValue = selectedQuest;

                    QuestInfo selectedQuestInfo = quests[selectedQuest];
                    int maxStep = selectedQuestInfo != null ? Mathf.Max(selectedQuestInfo.StepCount - 1, 0) : 0;
                    _selectedStepIndex.intValue = EditorGUILayout.IntSlider("Selected Step", _selectedStepIndex.intValue, 0, maxStep);
                    return;
                }
            }

            EditorGUILayout.PropertyField(_selectedQuestIndex);
            EditorGUILayout.PropertyField(_selectedStepIndex);
            EditorGUILayout.HelpBox("In Play Mode this switches to a quest dropdown built from QuestManager.AllQuests.", MessageType.Info);
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime Actions", EditorStyles.boldLabel);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to apply progress presets and reset quest runtime state.", MessageType.Info);
                    return;
                }

                var controller = (PortfolioQuestProgressController)target;

                if (GUILayout.Button("Reset Progress Now"))
                {
                    controller.ResetProgressNow();
                    EditorUtility.SetDirty(controller);
                }

                if (GUILayout.Button("Apply Selected Progress"))
                {
                    controller.ApplySelectedProgressNow();
                    EditorUtility.SetDirty(controller);
                }
            }
        }
    }
}
