using System;
using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.SaveSystem
{
    /// <summary>
    /// Entry-point MonoBehaviour for the save system.
    /// Attach to a persistent GameObject that loads before any scene that reads or
    /// writes player data.
    /// </summary>
    [AddComponentMenu("SaveSystem/Save System Bootstrapper")]
    public sealed class SaveSystemBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            EnsureLogServiceRegistered();
            BuildAndRegisterSaveSystem();
            InitializeUgsAsync().Forget();
        }

        private async UniTaskVoid InitializeUgsAsync()
        {
            try
            {
                await UnityServices.InitializeAsync().AsUniTask();
                AppLog.Info("[SaveSystemBootstrapper] Unity Gaming Services initialised.");
            }
            catch (Exception ex)
            {
                AppLog.Error(
                    $"[SaveSystemBootstrapper] UGS initialisation failed. " +
                    $"Cloud save calls may not work this session.\n{ex}");
            }
        }

        private static void EnsureLogServiceRegistered()
        {
            if (ServiceLocator.Global.TryGet(out ILogService _))
                return;

            var logService = new LogService(LogLevel.None);
            ServiceLocator.Global.Register<ILogService>(logService);
            AppLog.Register(logService);
        }

        private static void BuildAndRegisterSaveSystem()
        {
            var migrationService = new SaveMigrationService();
            var local = new LocalSaveService(migrationService);
            var cloud = new CloudPlayerSaveService(migrationService);
            var hybrid = new HybridSaveService(local, cloud);

            ServiceLocator.Global
                .Register<ISaveService>(hybrid)
                .Register<SaveMigrationService>(migrationService);

            AppLog.Info("[SaveSystemBootstrapper] Save system registered on ServiceLocator.Global.");
        }
    }
}
