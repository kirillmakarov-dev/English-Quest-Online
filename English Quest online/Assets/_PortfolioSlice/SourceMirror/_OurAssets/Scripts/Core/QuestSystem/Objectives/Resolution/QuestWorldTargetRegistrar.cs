using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.QuestSystem
{
    [AddComponentMenu(QuestSystemComponentMenuPaths.Objectives + "/World Target Registrar")]
    public class QuestWorldTargetRegistrar : MonoBehaviour
    {
        QuestWorldTargetRegistry _registry;
        bool _ownsRegistry;

        void Awake()
        {
            if (_registry != null)
                return;

            if (ServiceLocator.For(this).TryGet(out IQuestWorldTargetRegistry existing))
            {
                _registry = existing as QuestWorldTargetRegistry;
                _ownsRegistry = false;
                return;
            }

            _registry = new QuestWorldTargetRegistry();
            _ownsRegistry = true;
            ServiceLocator.For(this).Register<IQuestWorldTargetRegistry>(_registry);
        }

        void OnDestroy()
        {
            if (!_ownsRegistry)
                return;

            ServiceLocator.DeregisterFor<IQuestWorldTargetRegistry>(this);
            _ownsRegistry = false;
            _registry = null;
        }
    }
}

