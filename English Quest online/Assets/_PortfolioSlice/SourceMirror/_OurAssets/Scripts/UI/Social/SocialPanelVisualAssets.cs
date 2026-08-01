using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SocialPanelVisualAssets",
    menuName = ScriptableObjectMenuPaths.UI + "/Social Panel Visual Assets")]
public class SocialPanelVisualAssets : ScriptableObject
{
    [Header("Panel")]
    public Sprite PanelBackground;
    public Sprite TitleRibbon;
    public Sprite ListRowBackground;
    public Sprite InviteBannerBackground;

    [Header("Buttons")]
    public Sprite InviteButton;
    public Sprite AcceptButton;
    public Sprite DeclineButton;
    public Sprite KickButton;
    public Sprite CloseButton;
    public Sprite LeaveButton;

    [Header("Icons")]
    public Sprite AvatarFrame;
    public Sprite OnlineIndicator;
    public Sprite LeaderCrown;

    [Header("Typography")]
    public TMP_FontAsset TitleFont;
    public TMP_FontAsset BodyFont;

    [Header("Colors")]
    public Color TitleTextColor = new(0.98f, 0.93f, 0.78f, 1f);
    public Color BodyTextColor = Color.white;
    public Color MutedTextColor = new(0.66f, 0.72f, 0.82f, 1f);
    public Color InviteBannerTint = new(0.18f, 0.42f, 0.28f, 0.95f);
}
