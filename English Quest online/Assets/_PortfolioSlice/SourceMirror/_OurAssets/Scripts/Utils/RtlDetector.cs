using TMPro;

/// <summary>
/// Utility for detecting Hebrew text and applying the correct TextMeshPro RTL/LTR settings.
/// </summary>
public static class RtlDetector
{
    /// <summary>
    /// Returns true if <paramref name="text"/> contains at least one character
    /// from the Unicode Hebrew block (U+0590 – U+05FF).
    /// </summary>
    public static bool ContainsHebrew(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        foreach (char c in text)
        {
            if (c >= '\u0590' && c <= '\u05FF')
                return true;
        }

        return false;
    }

    /// <summary>
    /// Applies RTL or LTR settings to <paramref name="label"/> based on
    /// whether <paramref name="text"/> contains Hebrew characters.
    /// Accepts any <see cref="TMP_Text"/> subclass (TextMeshProUGUI, TextMeshPro, etc.).
    /// Only the horizontal alignment is flipped so Midline/Top/Bottom vertical
    /// alignment is preserved. Centered horizontal text is left alone.
    /// </summary>
    public static void Apply(TMP_Text label, string text)
    {
        if (label == null) return;

        bool isHebrew = ContainsHebrew(text);
        label.isRightToLeftText = isHebrew;

        // Don't override centered text — it works for both RTL and LTR.
        if (label.horizontalAlignment == HorizontalAlignmentOptions.Center)
            return;

        label.horizontalAlignment = isHebrew
            ? HorizontalAlignmentOptions.Right
            : HorizontalAlignmentOptions.Left;
    }
}
