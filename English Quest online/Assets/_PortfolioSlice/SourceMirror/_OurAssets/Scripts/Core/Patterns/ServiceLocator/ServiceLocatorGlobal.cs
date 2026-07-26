using UnityEngine;

namespace UnityServiceLocator {
    [AddComponentMenu("ServiceLocator/ServiceLocator Global")]
    [DefaultExecutionOrder(-100)] // Ensure this runs before any scene-level ServiceLocator to avoid conflicts
    public class ServiceLocatorGlobal : Bootstrapper {
        [SerializeField] bool dontDestroyOnLoad = true;
        
        protected override void Bootstrap() {
            Container.ConfigureAsGlobal(dontDestroyOnLoad);
        }
    }
}