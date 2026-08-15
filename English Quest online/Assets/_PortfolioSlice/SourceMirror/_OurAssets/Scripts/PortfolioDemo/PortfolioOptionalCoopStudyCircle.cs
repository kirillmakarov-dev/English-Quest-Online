using Fusion;
using TMPro;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    [AddComponentMenu("English Quest/Portfolio Demo/Optional Co-op Study Circle")]
    public sealed class PortfolioOptionalCoopStudyCircle : MonoBehaviour
    {
        private const string RingObjectName = "Ring";
        private const string LabelObjectName = "Label";
        private const float RefreshInterval = 0.25f;

        [SerializeField] private string activityName = "Study Circle";
        [SerializeField] [Min(1f)] private float radius = 2.25f;
        [SerializeField] [Min(2)] private int requiredPlayers = 2;
        [SerializeField] private Transform anchor;
        [SerializeField] private MeshRenderer ringRenderer;
        [SerializeField] private TextMeshPro label;
        [SerializeField] private Color idleColor = new(0.18f, 0.5f, 0.55f, 0.92f);
        [SerializeField] private Color waitingColor = new(0.96f, 0.74f, 0.23f, 0.92f);
        [SerializeField] private Color activeColor = new(0.23f, 0.84f, 0.58f, 0.96f);

        private float nextRefreshTime;
        private MaterialPropertyBlock ringProperties;

        public string ActivityName => string.IsNullOrWhiteSpace(activityName) ? "Study Circle" : activityName.Trim();

        private void Awake()
        {
            ResolveSceneReferences();
            RefreshVisualState(force: true);
        }

        private void OnValidate()
        {
            radius = Mathf.Max(1f, radius);
            requiredPlayers = Mathf.Max(2, requiredPlayers);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RefreshVisualState(force: false);
        }

        public static PortfolioOptionalCoopStudyCircle FindSceneInstance()
        {
            return FindFirstObjectByType<PortfolioOptionalCoopStudyCircle>(
                FindObjectsInactive.Include);
        }

        public bool TryGetSnapshot(NetworkRunner runner, PlayerRef localPlayer, out PortfolioOptionalCoopActivitySnapshot snapshot)
        {
            if (runner == null || !runner.IsRunning)
            {
                snapshot = default;
                return false;
            }

            int connectedPlayers = 0;
            int playersInside = 0;
            bool isLocalPlayerInside = false;
            Vector3 center = GetAnchorPosition();
            float squaredRadius = radius * radius;

            foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
            {
                connectedPlayers++;
                NetworkObject playerObject = PortfolioSessionPlayerUtility.ResolvePlayerObject(runner, player);
                if (playerObject == null)
                    continue;

                Vector3 playerPosition = playerObject.transform.position;
                float squaredDistance =
                    (playerPosition.x - center.x) * (playerPosition.x - center.x) +
                    (playerPosition.z - center.z) * (playerPosition.z - center.z);

                if (squaredDistance > squaredRadius)
                    continue;

                playersInside++;
                if (player == localPlayer)
                    isLocalPlayerInside = true;
            }

            bool isGroupActive = connectedPlayers > 1 && playersInside >= requiredPlayers;
            snapshot = new PortfolioOptionalCoopActivitySnapshot(
                ActivityName,
                connectedPlayers,
                playersInside,
                requiredPlayers,
                isLocalPlayerInside,
                isGroupActive);
            return true;
        }

        private void RefreshVisualState(bool force)
        {
            NetworkRunner runner = ResolveRunner();
            if (!force && runner == null)
                return;

            ResolveSceneReferences();

            PortfolioOptionalCoopActivitySnapshot snapshot;
            bool hasSnapshot = TryGetSnapshot(runner, runner != null ? runner.LocalPlayer : default, out snapshot);

            if (!hasSnapshot)
            {
                ApplyVisualState(
                    idleColor,
                    $"{ActivityName}\nOptional co-op marker");
                return;
            }

            string line = PortfolioOptionalCoopActivityFormatter.BuildBriefingLine(snapshot);
            Color color = snapshot.IsGroupActive
                ? activeColor
                : snapshot.PlayersInside > 0
                    ? waitingColor
                    : idleColor;

            ApplyVisualState(color, $"{ActivityName}\n{line}");
        }

        private void ApplyVisualState(Color color, string text)
        {
            if (ringRenderer != null)
            {
                ringProperties ??= new MaterialPropertyBlock();
                ringRenderer.GetPropertyBlock(ringProperties);
                ringProperties.SetColor("_BaseColor", color);
                ringProperties.SetColor("_Color", color);
                ringRenderer.SetPropertyBlock(ringProperties);
            }

            if (label != null)
            {
                label.color = color;
                label.text = text;
            }
        }

        private Vector3 GetAnchorPosition()
        {
            if (anchor == null)
                anchor = transform;

            return anchor.position;
        }

        private void ResolveSceneReferences()
        {
            if (anchor == null)
                anchor = transform;

            if (ringRenderer == null)
                ringRenderer = FindRingRenderer();

            if (label == null)
                label = FindLabel();

        }

        private MeshRenderer FindRingRenderer()
        {
            Transform ringTransform = transform.Find(RingObjectName);
            return ringTransform != null ? ringTransform.GetComponent<MeshRenderer>() : null;
        }

        private TextMeshPro FindLabel()
        {
            Transform labelTransform = transform.Find(LabelObjectName);
            return labelTransform != null ? labelTransform.GetComponent<TextMeshPro>() : null;
        }

        private static NetworkRunner ResolveRunner()
        {
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning && runner.ProvideInput)
                    return runner;
            }

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    return runner;
            }

            return null;
        }
    }
}
