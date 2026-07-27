using System;

namespace EnglishQuest.QuestSystem
{
    [Serializable]
    public struct ObjectiveProgress
    {
        public int Current;
        public int Target;
        public QuestStepStatus Status;

        public bool IsComplete => Status == QuestStepStatus.COMPLETED || (Target > 0 && Current >= Target);

        public static ObjectiveProgress NotStarted(int target)
        {
            int t = Math.Max(target, 1);
            return new ObjectiveProgress { Current = 0, Target = t, Status = QuestStepStatus.NOT_STARTED };
        }

        public static ObjectiveProgress Completed(int target)
        {
            int t = Math.Max(target, 1);
            return new ObjectiveProgress { Current = t, Target = t, Status = QuestStepStatus.COMPLETED };
        }
    }

    public readonly struct QuestObjectiveProgressEvent
    {
        public QuestInfo Quest { get; }
        public int StepIndex { get; }
        public ObjectiveProgress Progress { get; }

        public QuestObjectiveProgressEvent(QuestInfo quest, int stepIndex, ObjectiveProgress progress)
        {
            Quest = quest;
            StepIndex = stepIndex;
            Progress = progress;
        }
    }
}

