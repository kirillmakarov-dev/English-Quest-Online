using TMPro;
using UnityEngine;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Active Quest Journal Entry View")]
public class ActiveQuestJournalEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text objectiveLabel;
    [SerializeField] private TMP_Text progressLabel;

    public void Bind(ActiveQuestJournalEntry entry)
    {
        if (entry == null)
            return;

        if (titleLabel != null)
        {
            titleLabel.text = entry.DisplayName;
            RtlDetector.Apply(titleLabel, entry.DisplayName);
        }

        if (objectiveLabel != null)
        {
            objectiveLabel.text = entry.ObjectiveText;
            RtlDetector.Apply(objectiveLabel, entry.ObjectiveText);
        }

        if (progressLabel != null)
        {
            bool hasProgress = !string.IsNullOrEmpty(entry.ProgressText);
            progressLabel.gameObject.SetActive(hasProgress);
            if (hasProgress)
            {
                progressLabel.text = entry.ProgressText;
                RtlDetector.Apply(progressLabel, entry.ProgressText);
            }
        }
    }
}
