using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class FusionPhysicsSceneCameraCollision : CinemachineExtension
{
    private const string CameraObstacleLayerName = "CameraObstacle";

    private static int s_lastPhysicsSyncFrame = -1;

    [SerializeField, Min(0f)] private float _surfaceOffset = 0.03f;

    private readonly RaycastHit[] _hits = new RaycastHit[32];
    private bool _hasLoggedAttachment;
    private bool _hasLoggedFirstCollision;
    private bool _hasLoggedPhysicsScenes;
    private float _cameraCollisionCorrection;

    public static FusionPhysicsSceneCameraCollision EnsureOn(CinemachineCamera camera)
    {
        if (camera == null)
            return null;

        if (!camera.TryGetComponent(out FusionPhysicsSceneCameraCollision collision))
            collision = camera.gameObject.AddComponent<FusionPhysicsSceneCameraCollision>();

        collision.enabled = true;
        if (!collision._hasLoggedAttachment)
        {
            collision._hasLoggedAttachment = true;
            Debug.Log(
                $"[FusionPhysicsSceneCameraCollision] Attached to '{camera.name}' in scene " +
                $"'{camera.gameObject.scene.name}' (handle {camera.gameObject.scene.handle}).",
                collision);
        }

        return collision;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body || vcam == null)
            return;

        Transform target = vcam.Follow != null ? vcam.Follow : vcam.LookAt;
        if (target == null)
            return;

        Scene targetScene = target.gameObject.scene;
        CinemachineThirdPersonFollow thirdPersonFollow = vcam.GetComponent<CinemachineThirdPersonFollow>();
        if (thirdPersonFollow != null && !thirdPersonFollow.AvoidObstacles.Enabled)
            return;

        float radius = thirdPersonFollow != null
            ? Mathf.Max(0.01f, thirdPersonFollow.AvoidObstacles.CameraRadius)
            : 0.24f;
        int collisionMask = thirdPersonFollow != null
            ? thirdPersonFollow.AvoidObstacles.CollisionFilter.value
            : Physics.DefaultRaycastLayers;
        int cameraObstacleLayer = LayerMask.NameToLayer(CameraObstacleLayerName);
        if (cameraObstacleLayer >= 0)
            collisionMask |= 1 << cameraObstacleLayer;
        string ignoreTag = thirdPersonFollow != null
            ? thirdPersonFollow.AvoidObstacles.IgnoreTag
            : string.Empty;

        if (collisionMask == 0)
            return;

        Vector3 rigRoot = target.position;
        Vector3 collisionOrigin = target.position;
        if (thirdPersonFollow != null)
        {
            thirdPersonFollow.GetRigPositions(out rigRoot, out _, out Vector3 hand);
            collisionOrigin = hand;
        }

        Vector3 cameraPosition = state.GetCorrectedPosition();
        SynchronizePhysicsTransformsOncePerFrame();
        NetworkObject targetNetworkObject = target.GetComponentInParent<NetworkObject>();
        Transform targetRoot = targetNetworkObject != null
            ? targetNetworkObject.transform
            : target.root;
        PhysicsScene targetPhysicsScene = PhysicsSceneQueries.Resolve(targetScene);

        if (thirdPersonFollow != null)
        {
            collisionOrigin = ResolveRigSegment(
                targetPhysicsScene,
                rigRoot,
                collisionOrigin,
                radius * 1.05f,
                collisionMask,
                ignoreTag,
                targetRoot);
        }

        Vector3 direction = cameraPosition - collisionOrigin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return;

        direction /= distance;
        float desiredCorrection = 0f;
        if (TryGetNearestHit(
                targetPhysicsScene,
                collisionOrigin,
                radius,
                direction,
                distance,
                collisionMask,
                ignoreTag,
                targetRoot,
                out RaycastHit nearestHit))
        {
            float correctedDistance = Mathf.Max(0f, nearestHit.distance - _surfaceOffset);
            desiredCorrection = distance - correctedDistance;
        }

        if (!_hasLoggedPhysicsScenes)
        {
            _hasLoggedPhysicsScenes = true;
            Debug.Log(
                $"[FusionPhysicsSceneCameraCollision] Querying runner physics scene only. " +
                $"Player scene: '{targetScene.name}' (handle {targetScene.handle}, path '{targetScene.path}'); " +
                $"target physics: {targetPhysicsScene}; ignored player hierarchy: '{targetRoot.name}'.",
                this);
        }

        float damping = desiredCorrection > _cameraCollisionCorrection && thirdPersonFollow != null
            ? thirdPersonFollow.AvoidObstacles.DampingIntoCollision
            : thirdPersonFollow != null
                ? thirdPersonFollow.AvoidObstacles.DampingFromCollision
                : 0f;

        _cameraCollisionCorrection += deltaTime < 0f
            ? desiredCorrection - _cameraCollisionCorrection
            : Damper.Damp(desiredCorrection - _cameraCollisionCorrection, damping, deltaTime);
        _cameraCollisionCorrection = Mathf.Clamp(_cameraCollisionCorrection, 0f, distance);

        if (_cameraCollisionCorrection > 0.001f)
            state.PositionCorrection -= direction * _cameraCollisionCorrection;
    }

    private Vector3 ResolveRigSegment(
        PhysicsScene physicsScene,
        Vector3 origin,
        Vector3 destination,
        float radius,
        int collisionMask,
        string ignoreTag,
        Transform targetRoot)
    {
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return destination;

        direction /= distance;
        if (!TryGetNearestHit(
                physicsScene,
                origin,
                radius,
                direction,
                distance,
                collisionMask,
                ignoreTag,
                targetRoot,
                out RaycastHit nearestHit))
        {
            return destination;
        }

        float correctedDistance = Mathf.Max(0f, nearestHit.distance - _surfaceOffset);
        return origin + direction * correctedDistance;
    }

    private bool TryGetNearestHit(
        PhysicsScene physicsScene,
        Vector3 origin,
        float radius,
        Vector3 direction,
        float distance,
        int collisionMask,
        string ignoreTag,
        Transform targetRoot,
        out RaycastHit nearestHit)
    {
        nearestHit = default;
        if (!physicsScene.IsValid())
            return false;

        int hitCount = physicsScene.SphereCast(
            origin,
            radius,
            direction,
            _hits,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = _hits[i].collider;
            if (collider == null)
                continue;

            if (targetRoot != null && collider.transform.IsChildOf(targetRoot))
                continue;

            if (!string.IsNullOrEmpty(ignoreTag) && collider.CompareTag(ignoreTag))
                continue;

            if (_hits[i].distance >= nearestDistance)
                continue;

            nearestDistance = _hits[i].distance;
            nearestHit = _hits[i];
            if (!_hasLoggedFirstCollision)
            {
                _hasLoggedFirstCollision = true;
                Debug.Log(
                    $"[FusionPhysicsSceneCameraCollision] First camera collision: " +
                    $"'{collider.name}' in scene '{collider.gameObject.scene.name}', " +
                    $"physics {physicsScene}, distance {_hits[i].distance:F3}.",
                    collider);
            }
        }

        return nearestDistance < float.PositiveInfinity;
    }

    private static void SynchronizePhysicsTransformsOncePerFrame()
    {
        if (s_lastPhysicsSyncFrame == Time.frameCount)
            return;

        s_lastPhysicsSyncFrame = Time.frameCount;
        Physics.SyncTransforms();
    }
}
