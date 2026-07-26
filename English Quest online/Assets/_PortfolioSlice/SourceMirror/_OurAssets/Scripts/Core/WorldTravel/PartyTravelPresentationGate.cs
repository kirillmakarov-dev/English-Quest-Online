using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// Synchronizes party follow-travel with the leader's shared map presentation
/// (animation + fade) when multiple peers share one DontDestroyOnLoad WorldMapUI.
/// </summary>
internal static class PartyTravelPresentationGate
{
    private static readonly Dictionary<int, UniTaskCompletionSource> _gates = new();
    private static int _currentEpoch;

    public static int BeginPresentation()
    {
        _currentEpoch++;
        _gates[_currentEpoch] = new UniTaskCompletionSource();
        return _currentEpoch;
    }

    public static void SignalPresentationComplete(int presentationEpoch)
    {
        if (_gates.TryGetValue(presentationEpoch, out UniTaskCompletionSource gate))
            gate.TrySetResult();
    }

    public static void Reset(int presentationEpoch)
    {
        if (presentationEpoch > 0)
            _gates.Remove(presentationEpoch);
    }

    public static void ClearAll()
    {
        _gates.Clear();
        _currentEpoch = 0;
    }

    public static async UniTask WaitForPresentationCompleteAsync(int presentationEpoch)
    {
        if (presentationEpoch <= 0)
            return;

        float elapsed = 0f;
        const float registerTimeoutSeconds = 10f;

        while (!_gates.ContainsKey(presentationEpoch) && elapsed < registerTimeoutSeconds)
        {
            elapsed += UnityEngine.Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        if (!_gates.TryGetValue(presentationEpoch, out UniTaskCompletionSource gate))
        {
            AppLog.Warning(
                $"[PartyTravelPresentationGate] Presentation epoch {presentationEpoch} was never registered.");
            return;
        }

        await gate.Task;
    }
}
