using UnityEngine;

namespace EnglishQuest.QuestSystem
{
    [AddComponentMenu(QuestSystemComponentMenuPaths.Markers + "/Area Marker")]
    public class QuestAreaMarker : MonoBehaviour
    {
        [SerializeField] private string areaId;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Collider triggerZone;

        Transform _indicatorTarget;

        public string AreaId => areaId;

        private void Awake()
        {
            if (triggerZone == null)
                triggerZone = GetComponent<Collider>();

            if (triggerZone != null)
                triggerZone.isTrigger = true;

            _indicatorTarget = CreateIndicatorTarget();
        }

        private void OnEnable()
        {
            QuestWorldTargetRegistration.TryRegister(this, QuestObjectiveType.EnterArea, areaId, _indicatorTarget);
        }

        private void OnDisable()
        {
            QuestWorldTargetRegistration.TryUnregister(this, QuestObjectiveType.EnterArea, areaId, _indicatorTarget);
        }

        Transform CreateIndicatorTarget()
        {
            if (triggerZone == null)
                return transform;

            var targetGo = new GameObject("IndicatorTarget");
            targetGo.transform.SetParent(transform, false);
            targetGo.transform.position = triggerZone.bounds.center;
            return targetGo.transform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag) || string.IsNullOrEmpty(areaId))
                return;

            QuestObjectiveEventBus.TryPublish(this, new QuestObjectiveEvents.AreaEntered(areaId));
        }
    }
}

