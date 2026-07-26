using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem.Data
{
    public sealed class StepProgressEntry
    {
        [JsonProperty("current")]
        public int Current { get; set; }

        [JsonProperty("target")]
        public int Target { get; set; } = 1;

        [JsonProperty("status")]
        public int Status { get; set; }
    }

    public sealed class QuestProgressEntry
    {
        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("currentStepIndex")]
        public int CurrentStepIndex { get; set; }

        [JsonProperty("steps")]
        public List<StepProgressEntry> Steps { get; set; } = new List<StepProgressEntry>();
    }
}
