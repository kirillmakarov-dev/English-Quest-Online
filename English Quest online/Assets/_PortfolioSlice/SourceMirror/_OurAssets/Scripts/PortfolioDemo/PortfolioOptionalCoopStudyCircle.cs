using Fusion;
using TMPro;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    [AddComponentMenu("English Quest/Portfolio Demo/Optional Co-op Study Circle")]
    public sealed class PortfolioOptionalCoopStudyCircle : MonoBehaviour
    {
        private const string DefaultObjectName = "Optional Co-op Study Circle";
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

        public string ActivityName => string.IsNullOrWhiteSpace(activityName) ? "Study Circle" : activityName.Trim();

        private void Awake()
        {
            EnsureRuntimeSetup();
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

        public static PortfolioOptionalCoopStudyCircle FindOrCreateRuntimeInstance()
        {
            PortfolioOptionalCoopStudyCircle existing = FindFirstObjectByType<PortfolioOptionalCoopStudyCircle>(
                FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            GameObject root = new(DefaultObjectName);
            root.transform.position = new Vector3(0f, 0.03f, 2.2f);
            PortfolioOptionalCoopStudyCircle created = root.AddComponent<PortfolioOptionalCoopStudyCircle>();
            created.EnsureRuntimeSetup();
            return created;
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

            EnsureRuntimeSetup();

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
            if (ringRenderer != null && ringRenderer.sharedMaterial != null)
                ringRenderer.sharedMaterial.color = color;

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

        private void EnsureRuntimeSetup()
        {
            if (anchor == null)
                anchor = transform;

            if (ringRenderer == null)
                ringRenderer = FindRingRenderer();

            if (label == null)
                label = FindLabel();

            if (ringRenderer == null)
                ringRenderer = CreateRingRenderer();

            if (label == null)
                label = CreateLabel();
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

        private MeshRenderer CreateRingRenderer()
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = RingObjectName;
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            Collider ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
                Destroy(ringCollider);

            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.sharedMaterial = new Material(shader)
            {
                color = idleColor,
                name = "Optional Co-op Study Circle Material"
            };

            return renderer;
        }

        private TextMeshPro CreateLabel()
        {
            GameObject labelObject = new(LabelObjectName, typeof(TextMeshPro));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(25f, 180f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.18f;

            TextMeshPro createdLabel = labelObject.GetComponent<TextMeshPro>();
            createdLabel.fontSize = 4.5f;
            createdLabel.alignment = TextAlignmentOptions.Center;
            createdLabel.rectTransform.sizeDelta = new Vector2(15f, 5f);
            createdLabel.textWrappingMode = TextWrappingModes.Normal;
            return createdLabel;
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
