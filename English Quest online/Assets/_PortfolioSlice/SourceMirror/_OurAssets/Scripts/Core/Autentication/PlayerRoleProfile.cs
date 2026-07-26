using UnityEngine;

/// <summary>
/// Cached player role from login (Cloud Code whitelist / dev bootstrap).
/// Used to decide whether the local client may claim session guider on join.
/// </summary>
public static class PlayerRoleProfile
{
    public const string RoleGuider = "guider";
    public const string RoleStudent = "student";

    private const string PlayerPrefsKey = "playerRole";

    public static bool IsGuider
    {
        get
        {
            var role = PlayerPrefs.GetString(PlayerPrefsKey, RoleStudent);
            return role == RoleGuider || role == "teacher";
        }
    }

    public static void SetGuider(bool isGuider)
    {
        SetRole(isGuider ? RoleGuider : RoleStudent);
    }

    public static void SetRole(string role)
    {
        PlayerPrefs.SetString(PlayerPrefsKey, string.IsNullOrEmpty(role) ? RoleStudent : role);
        PlayerPrefs.Save();
        AppLog.Info($"[PlayerRoleProfile] Role set to: {PlayerPrefs.GetString(PlayerPrefsKey)}");
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();
    }
}
