using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Registers <see cref="ILogService"/> on the global ServiceLocator at boot.
/// Optional — <see cref="SaveSystem.SaveSystemBootstrapper"/> also registers a default instance.
/// </summary>
[AddComponentMenu("Logging/Log Service Bootstrapper")]
[DefaultExecutionOrder(-98)]
public sealed class LogServiceBootstrapper : MonoBehaviour
{
    [SerializeField] LogLevel initialLevel = LogLevel.None;
    [SerializeField] ScriptableObject debugConfig;

    private const string DebugConfigResourcePath = "Editor/LogServiceDebugConfig";

    void Awake()
    {
        LogLevel resolvedLevel = ResolveInitialLevel();

        if (ServiceLocator.Global.TryGet(out ILogService existing))
        {
            if (existing is LogService logService)
                logService.Level = resolvedLevel;
            AppLog.Register(existing);
            return;
        }

        var service = new LogService(resolvedLevel);
        ServiceLocator.Global.Register<ILogService>(service);
        AppLog.Register(service);
    }

    private LogLevel ResolveInitialLevel()
    {
        if (debugConfig != null)
            return ReadLevelFromConfig(debugConfig);

        var loadedDebugConfig = Resources.Load<ScriptableObject>(DebugConfigResourcePath);
        return loadedDebugConfig != null ? ReadLevelFromConfig(loadedDebugConfig) : initialLevel;
    }

    private LogLevel ReadLevelFromConfig(ScriptableObject config)
    {
        var levelField = config.GetType().GetField("initialLevel");
        if (levelField != null && levelField.FieldType == typeof(LogLevel))
            return (LogLevel)levelField.GetValue(config);

        return initialLevel;
    }
}
