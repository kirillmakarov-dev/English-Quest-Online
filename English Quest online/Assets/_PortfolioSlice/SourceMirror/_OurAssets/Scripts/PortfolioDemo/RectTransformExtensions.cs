using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    internal static class PortfolioThemeResources
    {
        private const string ResourceRoot = "UI/PortfolioTheme/";

        public static Sprite HeaderCardSprite => LoadSprite("dialogue_panel");
        public static Sprite BriefingCardSprite => LoadSprite("mission_card");
        public static Sprite PlayerCardSprite => LoadSprite("player_card");
        public static Sprite CompletionCardSprite => LoadSprite("completion_panel");
        public static Sprite DialogueCardSprite => LoadSprite("dialogue_panel");
        public static Sprite PrimaryButtonSprite => LoadSprite("button_primary");
        public static Sprite SecondaryButtonSprite => LoadSprite("button_secondary");

        public static void ApplyPanelSprite(Image image, Sprite sprite, Color? tint = null)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = tint ?? Color.white;
        }

        public static void ApplyPrimaryButtonStyle(Button button)
        {
            ApplyButtonStyle(button, PrimaryButtonSprite, new Color(1f, 0.97f, 0.92f, 1f));
        }

        public static void ApplySecondaryButtonStyle(Button button)
        {
            ApplyButtonStyle(button, SecondaryButtonSprite, new Color(0.93f, 0.97f, 1f, 1f));
        }

        public static void ApplyDialogueSurface(Image image, TextMeshProUGUI titleText, TextMeshProUGUI bodyText)
        {
            ApplyPanelSprite(image, DialogueCardSprite);

            if (titleText != null)
            {
                titleText.color = new Color(1f, 0.82f, 0.34f, 1f);
                titleText.fontStyle = FontStyles.Bold;
            }

            if (bodyText != null)
            {
                bodyText.color = new Color(0.96f, 0.98f, 1f, 1f);
                bodyText.fontStyle = FontStyles.Normal;
            }
        }

        private static void ApplyButtonStyle(Button button, Sprite sprite, Color textColor)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            ApplyPanelSprite(image, sprite);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            colors.pressedColor = new Color(0.88f, 0.9f, 0.95f, 0.92f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            colors.fadeDuration = 0.15f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = textColor;
                label.fontStyle = FontStyles.Bold;
                label.fontSize = Mathf.Max(label.fontSize, 20f);
            }
        }

        private static Sprite LoadSprite(string assetName)
        {
            return Resources.Load<Sprite>(ResourceRoot + assetName);
        }
    }
}
