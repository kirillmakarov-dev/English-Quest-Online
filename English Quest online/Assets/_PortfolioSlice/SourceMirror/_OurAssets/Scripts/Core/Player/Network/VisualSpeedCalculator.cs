using UnityEngine;

/// <summary>
/// Calculates a smoothed, clamped horizontal speed from visual position delta.
/// Using position delta (not rigidbody velocity) is more reliable for network proxies.
/// </summary>
public struct VisualSpeedCalculator
{
    private float _smoothedSpeed;
    private Vector3 _lastPosition;

    public void Initialize(Vector3 startPosition)
    {
        _lastPosition = startPosition;
        _smoothedSpeed = 0f;
    }

    /// <summary>
    /// Advances the calculation by one frame and returns the new smoothed speed.
    /// Also advances the internal last-position tracker — call exactly once per Render frame.
    /// </summary>
    public float Compute(Vector3 currentPosition, float smoothing, float deadzone, float maxSpeed, float deltaTime)
    {
        float rawSpeed = 0f;
        if (deltaTime > 0.0001f)
        {
            Vector3 flatDelta = currentPosition - _lastPosition;
            flatDelta.y = 0f;
            rawSpeed = flatDelta.magnitude / deltaTime;
        }

        if (maxSpeed > 0f)
            rawSpeed = Mathf.Min(rawSpeed, maxSpeed);

        if (rawSpeed < deadzone)
            rawSpeed = 0f;

        float smoothFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * deltaTime);
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, rawSpeed, smoothFactor);
        _lastPosition = currentPosition;

        return _smoothedSpeed;
    }
}
