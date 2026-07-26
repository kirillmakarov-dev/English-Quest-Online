using UnityEngine;

namespace UnityServiceLocator {
    [AddComponentMenu("ServiceLocator/ServiceLocator Scene")]
    public class ServiceLocatorScene : Bootstrapper {
        protected override void Bootstrap() {
            RegisterLocalPlayerReadiness();
        }

        void RegisterLocalPlayerReadiness()
        {
            Container.ConfigureForScene();

            if (Container.TryGet(out ILocalPlayerReadiness _))
                return;

            LocalPlayerReadinessProvider readiness = GetComponent<LocalPlayerReadinessProvider>()
                ?? gameObject.AddComponent<LocalPlayerReadinessProvider>();
            Container.Register<ILocalPlayerReadiness>(readiness);
        }
    }
}
