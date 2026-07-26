using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LetterAudioDatabase", menuName = ScriptableObjectMenuPaths.CoreAudio + "/Letter Audio Database")]
public class LetterAudioDatabase : ScriptableObject
{
    [System.Serializable]
    public class LetterAudioEntry
    {
        public string character; // "A", "B", etc.
        public AudioClip clip;
    }

    [Tooltip("List mapping characters to their speech audio clips")]
    public List<LetterAudioEntry> letterClips;

    private Dictionary<string, AudioClip> _lookup;

    private void InitLookup()
    {
        _lookup = new Dictionary<string, AudioClip>();
        foreach (var entry in letterClips)
        {
            if (!string.IsNullOrEmpty(entry.character) && entry.clip != null)
            {
                string key = entry.character.ToUpper();
                if (!_lookup.ContainsKey(key))
                    _lookup.Add(key, entry.clip);
            }
        }
    }

    public AudioClip GetClipForLetter(string letter)
    {
#if UNITY_EDITOR
        InitLookup();
#else
        if (_lookup == null) InitLookup();
#endif

        string key = letter.ToUpper();
        if (_lookup.TryGetValue(key, out var clip))
            return clip;

        AppLog.Warning($"LetterAudioDatabase: No clip found for letter '{letter}'");
        return null;
    }
}
