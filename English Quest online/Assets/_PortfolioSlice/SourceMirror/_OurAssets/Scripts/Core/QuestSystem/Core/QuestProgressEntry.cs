using System;
using System.Collections.Generic;

namespace EnglishQuest.QuestSystem
{
    [Serializable]
    public sealed class QuestProgressEntry
    {
        public int State;
        public int CurrentStepIndex;
        public List<StepProgressEntry> Steps = new List<StepProgressEntry>();
    }

    [Serializable]
    public sealed class StepProgressEntry
    {
        public int Current;
        public int Target;
        public int Status;
    }
}

