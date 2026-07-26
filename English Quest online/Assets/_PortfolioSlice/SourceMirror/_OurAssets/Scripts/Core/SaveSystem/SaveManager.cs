using System;
using Cysharp.Threading.Tasks;
using EnglishKingdom.SaveSystem.Data;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Convenience facade over <see cref="ISaveService"/> for the live save domains.
    /// The service is resolved from <see cref="ServiceLocator.Global"/> on demand so
    /// scene reloads do not require re-wiring references.
    /// </summary>
    public static class SaveManager
    {
        private static ISaveService _service;

        private static ISaveService Service
        {
            get
            {
                if (_service != null)
                    return _service;

                try
                {
                    _service = ServiceLocator.Global.Get<ISaveService>();
                }
                catch (Exception ex)
                {
                    throw new SaveException(
                        SaveErrorCode.ServiceUnavailable,
                        "[SaveManager] ISaveService is not registered yet. Ensure SaveSystemBootstrapper has initialised before calling SaveManager.",
                        ex);
                }

                if (_service == null)
                {
                    throw new SaveException(
                        SaveErrorCode.ServiceUnavailable,
                        "[SaveManager] ISaveService resolved to null. Ensure SaveSystemBootstrapper is configured and initialised.");
                }

                return _service;
            }
        }

        /// <summary>Clears the cached service reference between editor play sessions.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _service = null;

        public static UniTask SaveCoreAsync(CoreSaveData data) =>
            Service.SaveAsync(CoreSaveData.Key, data);

        public static async UniTask<CoreSaveData> LoadCoreAsync()
        {
            CoreSaveData result = await Service.LoadAsync<CoreSaveData>(
                CoreSaveData.Key, CoreSaveData.CurrentVersion);
            return result ?? new CoreSaveData();
        }

        public static UniTask SaveSettingsAsync(SettingsSaveData data) =>
            Service.SaveAsync(SettingsSaveData.Key, data);

        public static async UniTask<SettingsSaveData> LoadSettingsAsync()
        {
            SettingsSaveData result = await Service.LoadAsync<SettingsSaveData>(
                SettingsSaveData.Key, SettingsSaveData.CurrentVersion);
            return result ?? new SettingsSaveData();
        }

        public static UniTask SaveProgressAsync(ProgressSaveData data) =>
            Service.SaveAsync(ProgressSaveData.Key, data);

        public static async UniTask<ProgressSaveData> LoadProgressAsync()
        {
            ProgressSaveData result = await Service.LoadAsync<ProgressSaveData>(
                ProgressSaveData.Key, ProgressSaveData.CurrentVersion);
            return result ?? new ProgressSaveData();
        }

        public static UniTask DeleteCoreAsync() =>
            Service.DeleteAsync(CoreSaveData.Key);

        public static UniTask DeleteSettingsAsync() =>
            Service.DeleteAsync(SettingsSaveData.Key);

        public static UniTask DeleteProgressAsync() =>
            Service.DeleteAsync(ProgressSaveData.Key);

        public static UniTask SaveCurrencyAsync(CurrencySaveData data) =>
            Service.SaveAsync(CurrencySaveData.Key, data);

        public static async UniTask<CurrencySaveData> LoadCurrencyAsync()
        {
            CurrencySaveData result = await Service.LoadAsync<CurrencySaveData>(
                CurrencySaveData.Key, CurrencySaveData.CurrentVersion);
            return result ?? new CurrencySaveData();
        }

        public static UniTask DeleteCurrencyAsync() =>
            Service.DeleteAsync(CurrencySaveData.Key);

        public static UniTask SaveAbilitiesAsync(AbilitySaveData data) =>
            Service.SaveAsync(AbilitySaveData.Key, data);

        public static async UniTask<AbilitySaveData> LoadAbilitiesAsync()
        {
            AbilitySaveData result = await Service.LoadAsync<AbilitySaveData>(
                AbilitySaveData.Key, AbilitySaveData.CurrentVersion);
            return result ?? new AbilitySaveData();
        }

        public static UniTask DeleteAbilitiesAsync() =>
            Service.DeleteAsync(AbilitySaveData.Key);

        public static UniTask SaveStatsAsync(StatsSaveData data) =>
            Service.SaveAsync(StatsSaveData.Key, data);

        public static async UniTask<StatsSaveData> LoadStatsAsync()
        {
            StatsSaveData result = await Service.LoadAsync<StatsSaveData>(
                StatsSaveData.Key, StatsSaveData.CurrentVersion);
            return result ?? new StatsSaveData();
        }

        public static UniTask DeleteStatsAsync() =>
            Service.DeleteAsync(StatsSaveData.Key);
    }
}
