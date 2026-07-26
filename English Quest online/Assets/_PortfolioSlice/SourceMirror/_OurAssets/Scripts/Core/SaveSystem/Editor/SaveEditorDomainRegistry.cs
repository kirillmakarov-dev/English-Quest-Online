using System;
using System.Collections.Generic;
using EnglishKingdom.SaveSystem.Data;

#if UNITY_EDITOR
namespace EnglishKingdom.SaveSystem.Editor
{
    /// <summary>
    /// Central registry of save domains exposed in the Save Data Inspector window.
    /// </summary>
    internal static class SaveEditorDomainRegistry
    {
        internal sealed class DomainDescriptor
        {
            public string DisplayName { get; }
            public string Key { get; }
            public Type DataType { get; }
            public int LatestVersion { get; }

            public DomainDescriptor(string displayName, string key, Type dataType, int latestVersion)
            {
                DisplayName = displayName;
                Key = key;
                DataType = dataType;
                LatestVersion = latestVersion;
            }

            public override string ToString() => DisplayName;
        }

        private static readonly IReadOnlyList<DomainDescriptor> s_all = new List<DomainDescriptor>
        {
            new DomainDescriptor("Core", CoreSaveData.Key, typeof(CoreSaveData), CoreSaveData.CurrentVersion),
            new DomainDescriptor("Settings", SettingsSaveData.Key, typeof(SettingsSaveData), SettingsSaveData.CurrentVersion),
            new DomainDescriptor("Progress", ProgressSaveData.Key, typeof(ProgressSaveData), ProgressSaveData.CurrentVersion),
            new DomainDescriptor("Currency", CurrencySaveData.Key, typeof(CurrencySaveData), CurrencySaveData.CurrentVersion),
            new DomainDescriptor("Abilities", AbilitySaveData.Key, typeof(AbilitySaveData), AbilitySaveData.CurrentVersion),
            new DomainDescriptor("Stats", StatsSaveData.Key, typeof(StatsSaveData), StatsSaveData.CurrentVersion)
        };

        public static IReadOnlyList<DomainDescriptor> All => s_all;
    }
}
#endif
