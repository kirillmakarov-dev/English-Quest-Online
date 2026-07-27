using UnityEngine;

public class NameTagBillboard : MonoBehaviour
{
    public enum FacingMode
    {
        /// <summary>+Z points at the camera position (legacy).</summary>
        TowardCamera = 0,
        /// <summary>Match the camera yaw so world Canvas / TextMesh reads like on screen.</summary>
        MatchCameraYaw = 1,
    }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private FacingMode facingMode = FacingMode.TowardCamera;
    [Tooltip("Extra local rotation applied after the billboard faces the camera.")]
    [SerializeField] private Vector3 rotationOffsetEuler;

    private Quaternion _rotationOffset;

    private void Awake()
    {
        _rotationOffset = Quaternion.Euler(rotationOffsetEuler);
    }

    void LateUpdate()
    {
        if (!TryResolveCamera(out Transform cam))
            return;

        transform.rotation = GetBillboardRotation(cam) * _rotationOffset;
    }

    public void SetCameraTransform(Transform newCameraTransform)
    {
        cameraTransform = newCameraTransform;
    }

    public void Configure(FacingMode newFacingMode, Vector3 newRotationOffsetEuler)
    {
        facingMode = newFacingMode;
        rotationOffsetEuler = newRotationOffsetEuler;
        _rotationOffset = Quaternion.Euler(rotationOffsetEuler);
    }

    private Quaternion GetBillboardRotation(Transform cam)
    {
        if (facingMode == FacingMode.MatchCameraYaw)
        {
            Vector3 euler = cam.eulerAngles;
            return Quaternion.Euler(0f, euler.y, 0f);
        }

        Vector3 direction = cam.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return transform.rotation;

        return Quaternion.LookRotation(direction);
    }

    private bool TryResolveCamera(out Transform cam)
    {
        if (cameraTransform != null)
        {
            cam = cameraTransform;
            return true;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
            cam = cameraTransform;
            return true;
        }

        GameObject taggedCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (taggedCamera != null)
        {
            cameraTransform = taggedCamera.transform;
            cam = cameraTransform;
            return true;
        }

        Camera fallbackCamera = FindFirstObjectByType<Camera>();
        if (fallbackCamera != null)
        {
            cameraTransform = fallbackCamera.transform;
            cam = cameraTransform;
            return true;
        }

        cam = null;
        return false;
    }
}
