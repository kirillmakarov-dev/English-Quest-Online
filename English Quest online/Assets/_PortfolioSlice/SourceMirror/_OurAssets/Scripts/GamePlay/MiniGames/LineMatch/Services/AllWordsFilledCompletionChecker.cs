namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class AllWordsFilledCompletionChecker : ICompletionChecker
    {
        public bool IsCompleted(LevelSession levelSession)
        {
            if (levelSession == null || levelSession.WordSlots.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < levelSession.WordSlots.Count; i++)
            {
                WordSlotState slotState = levelSession.WordSlots[i];
                if (slotState == null || !slotState.IsFilled)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
