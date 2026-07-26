using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Text;
#endif

/// <summary>
/// Game-side reader for the DPAPI-encrypted token written by the launcher.
///
/// The launcher encrypts a JSON payload {"idToken":"...","username":"...","gameVersion":"..."}
/// with DPAPI (CurrentUser scope), then writes it to %LOCALAPPDATA%\CourseGame\.auth.
///
/// Call ReadAndDestroyAll() to get both the Firebase ID token and the player username.
/// </summary>
public static class GameHandoffService
{
    public static readonly string HandoffPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CourseGame", ".auth");

    // Shared entropy constant — must match the launcher's GameHandoffService.
    private const string AppHandoffSecret = "EnglishKingdom-DPAPI-Handoff-v1";

    /// <summary>Holds both fields from the launcher handoff payload.</summary>
    public struct HandoffData
    {
        public string IdToken;
        public string Username;
        public string GameVersion;
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn,
        IntPtr ppszDataDescr,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        out DATA_BLOB pDataOut);
#endif

    // ─────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads, decrypts, parses, and immediately deletes the handoff file.
    /// Returns a <see cref="HandoffData"/> with both the Firebase ID token
    /// and the player username.  Both fields are empty strings on failure.
    /// </summary>
    public static HandoffData ReadAndDestroyAll()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!File.Exists(HandoffPath))
        {
            AppLog.Warning("[Handoff] Auth file not found. Was the game launched via the launcher?");
            return default;
        }

        try
        {
            byte[] encrypted = File.ReadAllBytes(HandoffPath);
            byte[] entropy   = Encoding.UTF8.GetBytes(AppHandoffSecret);
            string json      = DecryptDpapi(encrypted, entropy);
            return ParsePayload(json);
        }
        catch (Exception ex)
        {
            AppLog.Error($"[Handoff] Failed to decrypt auth token: {ex.Message}");
            return default;
        }
        finally
        {
            try { File.Delete(HandoffPath); } catch { /* best-effort */ }
        }
#else
        AppLog.Warning("[Handoff] GameHandoffService is only supported on Windows.");
        return default;
#endif
    }

    /// <summary>
    /// Convenience wrapper — returns only the Firebase ID token.
    /// Prefer <see cref="ReadAndDestroyAll"/> when you also need the username.
    /// </summary>
    public static string ReadAndDestroy()
    {
        return ReadAndDestroyAll().IdToken;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a JSON string of the form {"idToken":"...","username":"...","gameVersion":"..."}.
    /// Uses minimal string parsing so there is no external JSON dependency.
    /// </summary>
    private static HandoffData ParsePayload(string json)
    {
        var data = new HandoffData
        {
            IdToken     = ExtractJsonString(json, "idToken"),
            Username    = ExtractJsonString(json, "username"),
            GameVersion = ExtractJsonString(json, "gameVersion")
        };
        return data;
    }

    /// <summary>Extracts the string value for <paramref name="key"/> from a flat JSON object.</summary>
    private static string ExtractJsonString(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return string.Empty;
        string search = "\"" + key + "\":\"";
        int start = json.IndexOf(search, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        start += search.Length;
        int end = json.IndexOf('"', start);
        return end > start ? json.Substring(start, end - start) : string.Empty;
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static string DecryptDpapi(byte[] cipherData, byte[] entropy)
    {
        GCHandle cipherHandle  = GCHandle.Alloc(cipherData, GCHandleType.Pinned);
        GCHandle entropyHandle = GCHandle.Alloc(entropy,    GCHandleType.Pinned);
        try
        {
            DATA_BLOB inBlob = new DATA_BLOB
            {
                cbData = cipherData.Length,
                pbData = cipherHandle.AddrOfPinnedObject()
            };
            DATA_BLOB entropyBlob = new DATA_BLOB
            {
                cbData = entropy.Length,
                pbData = entropyHandle.AddrOfPinnedObject()
            };

            if (!CryptUnprotectData(ref inBlob, IntPtr.Zero, ref entropyBlob,
                    IntPtr.Zero, IntPtr.Zero, 0, out DATA_BLOB outBlob))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                byte[] decrypted = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, decrypted, 0, outBlob.cbData);
                return Encoding.UTF8.GetString(decrypted);
            }
            finally
            {
                Marshal.FreeHGlobal(outBlob.pbData);
            }
        }
        finally
        {
            cipherHandle.Free();
            entropyHandle.Free();
        }
    }
#endif
}
