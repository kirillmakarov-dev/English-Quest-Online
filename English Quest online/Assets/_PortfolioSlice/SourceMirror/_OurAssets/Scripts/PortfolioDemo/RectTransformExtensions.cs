using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    internal static class RectTransformExtensions
    {
        public static void StretchToParent(this RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
