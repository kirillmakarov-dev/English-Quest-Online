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

    [System.Serializable]
    internal sealed class PortfolioThemeSpriteSet
    {
        [SerializeField] private Sprite headerCard;
        [SerializeField] private Sprite briefingCard;
        [SerializeField] private Sprite playerCard;
        [SerializeField] private Sprite completionCard;
        [SerializeField] private Sprite dialogueCard;
        [SerializeField] private Sprite primaryButton;
        [SerializeField] private Sprite secondaryButton;

        public Sprite HeaderCard => headerCard;
        public Sprite BriefingCard => briefingCard;
        public Sprite PlayerCard => playerCard;
        public Sprite CompletionCard => completionCard;
        public Sprite DialogueCard => dialogueCard;
        public Sprite PrimaryButton => primaryButton;
        public Sprite SecondaryButton => secondaryButton;
    }

    internal static class PortfolioThemeResources
    {
        public static readonly Color WarmHeadingColor = new(1f, 0.82f, 0.34f, 1f);
        public static readonly Color BodyTextColor = new(0.96f, 0.98f, 1f, 1f);
        public static readonly Color AccentMintColor = new(0.56f, 0.95f, 0.82f, 1f);
        public static readonly Color MutedTextColor = new(0.79f, 0.88f, 0.93f, 0.95f);
        public static readonly Color TileGlowColor = new(0.18f, 0.72f, 0.68f, 0.94f);
        public static readonly Color SlotGlowColor = new(0.15f, 0.32f, 0.52f, 0.92f);
        public static readonly Color SlotFilledColor = new(0.17f, 0.48f, 0.42f, 0.92f);
        public static readonly Color LockedColor = new(0.73f, 0.75f, 0.8f, 1f);
        public static readonly Color TurnInColor = new(0.46f, 1f, 0.62f, 1f);
        public static readonly Color AvailableColor = new(1f, 0.83f, 0.22f, 1f);
        public static readonly Color InProgressColor = new(0.5f, 0.82f, 1f, 1f);
        private static readonly Color PanelSurfaceColor = new(0.02f, 0.09f, 0.11f, 0.9f);
        private static readonly Color PanelAccentColor = new(0.08f, 0.42f, 0.44f, 0.72f);
        private static readonly Color PrimaryButtonSurfaceColor = new(0.08f, 0.62f, 0.55f, 0.96f);
        private static readonly Color SecondaryButtonSurfaceColor = new(0.08f, 0.22f, 0.32f, 0.94f);

        private static PortfolioThemeSpriteSet currentSprites;

        public static Sprite HeaderCardSprite => currentSprites != null ? currentSprites.HeaderCard : null;
        public static Sprite BriefingCardSprite => currentSprites != null ? currentSprites.BriefingCard : null;
        public static Sprite PlayerCardSprite => currentSprites != null ? currentSprites.PlayerCard : null;
        public static Sprite CompletionCardSprite => currentSprites != null ? currentSprites.CompletionCard : null;
        public static Sprite DialogueCardSprite => currentSprites != null ? currentSprites.DialogueCard : null;
        public static Sprite PrimaryButtonSprite => currentSprites != null ? currentSprites.PrimaryButton : null;
        public static Sprite SecondaryButtonSprite => currentSprites != null ? currentSprites.SecondaryButton : null;

        public static void SetSprites(PortfolioThemeSpriteSet sprites)
        {
            currentSprites = sprites;
        }

        public static void ApplyPanelSprite(Image image, Sprite sprite, Color? tint = null)
        {
            if (image == null)
                return;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }

            image.preserveAspect = false;
            image.color = sprite != null ? tint ?? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        public static void ApplyPrimaryButtonStyle(Button button)
        {
            ApplyButtonStyle(button, PrimaryButtonSprite, new Color(1f, 0.97f, 0.92f, 1f), new Color(1f, 1f, 1f, 0.98f));
        }

        public static void ApplySecondaryButtonStyle(Button button)
        {
            ApplyButtonStyle(button, SecondaryButtonSprite, new Color(0.93f, 0.97f, 1f, 1f), new Color(1f, 1f, 1f, 0.92f));
        }

        public static void ApplyDialogueSurface(Image image, TextMeshProUGUI titleText, TextMeshProUGUI bodyText)
        {
            ApplyPanelSprite(image, DialogueCardSprite, new Color(1f, 1f, 1f, 0.98f));

            if (titleText != null)
            {
                titleText.color = WarmHeadingColor;
                titleText.fontStyle = FontStyles.Bold;
            }

            if (bodyText != null)
            {
                bodyText.color = BodyTextColor;
                bodyText.fontStyle = FontStyles.Normal;
            }
        }

        public static void ApplyMiniGameSurface(Image image)
        {
            ApplyPanelSprite(image, DialogueCardSprite, new Color(1f, 1f, 1f, 0.98f));
        }

        public static void ApplyTileSurface(Image image, TMP_Text label, bool isUsed)
        {
            if (image != null)
                ApplyPanelSprite(image, PrimaryButtonSprite, isUsed ? new Color(0.14f, 0.2f, 0.24f, 0.72f) : PrimaryButtonSurfaceColor);

            if (label != null)
            {
                label.color = isUsed ? new Color(0.82f, 0.85f, 0.9f, 0.85f) : WarmHeadingColor;
                label.fontStyle = FontStyles.Bold;
                label.fontSize = Mathf.Max(label.fontSize, 24f);
            }
        }

        public static void ApplySlotSurface(Image image, TMP_Text label, bool isFilled, bool isPreFilled)
        {
            if (image != null)
            {
                Color tint = isPreFilled
                    ? new Color(0.2f, 0.26f, 0.4f, 0.94f)
                    : isFilled
                        ? SlotFilledColor
                        : SecondaryButtonSurfaceColor;
                ApplyPanelSprite(image, SecondaryButtonSprite, tint);
            }

            if (label != null)
            {
                label.color = isFilled || isPreFilled ? BodyTextColor : AccentMintColor;
                label.fontStyle = FontStyles.Bold;
                label.fontSize = Mathf.Max(label.fontSize, 22f);
            }
        }

        public static void ApplySectionHeading(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.color = WarmHeadingColor;
            label.fontStyle = FontStyles.Bold;
        }

        public static void ApplyBodyLabel(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            label.color = BodyTextColor;
        }

        public static string GetNpcIndicatorText(QuestNpcIndicatorState state)
        {
            return state switch
            {
                QuestNpcIndicatorState.Available => "AVAILABLE",
                QuestNpcIndicatorState.InProgress => "ACTIVE",
                QuestNpcIndicatorState.TurnIn => "READY",
                QuestNpcIndicatorState.Locked => "LOCKED",
                _ => string.Empty
            };
        }

        public static Color GetNpcIndicatorColor(QuestNpcIndicatorState state)
        {
            return state switch
            {
                QuestNpcIndicatorState.Available => AvailableColor,
                QuestNpcIndicatorState.InProgress => InProgressColor,
                QuestNpcIndicatorState.TurnIn => TurnInColor,
                QuestNpcIndicatorState.Locked => LockedColor,
                _ => Color.white
            };
        }

        private static void ApplyButtonStyle(Button button, Sprite sprite, Color textColor, Color surfaceTint)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            ApplyPanelSprite(image, sprite, surfaceTint);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.96f, 0.92f, 1f);
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

    }
}
