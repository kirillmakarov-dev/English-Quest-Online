using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityServiceLocator {
    [DefaultExecutionOrder(-101)]
    public class ServiceLocator : MonoBehaviour {
        static ServiceLocator global;
        static readonly Dictionary<Scene, ServiceLocator> sceneContainers = new Dictionary<Scene, ServiceLocator>();
        
        readonly ServiceManager services = new ServiceManager();
        
        const string k_globalServiceLocatorName = "ServiceLocator [Global]";
        const string k_sceneServiceLocatorName = "ServiceLocator [Scene]";

        internal void ConfigureAsGlobal(bool dontDestroyOnLoad) {
            if (global == this) {
                Debug.LogWarning("ServiceLocator.ConfigureAsGlobal: Already configured as global", this);
            } else if (global != null) {
                Debug.LogWarning(
                    "ServiceLocator.ConfigureAsGlobal: Another ServiceLocator is already configured as global. Skipping duplicate.",
                    this);
            } else {
                global = this;
                if (dontDestroyOnLoad && Application.isPlaying)
                    DontDestroyOnLoad(gameObject);
            }
        }

        internal void ConfigureForScene() {
            Scene scene = gameObject.scene;
            if (!scene.IsValid())
                return;

            var staleScenes = new List<Scene>();
            foreach (KeyValuePair<Scene, ServiceLocator> entry in sceneContainers)
            {
                if (entry.Value == this && entry.Key != scene)
                    staleScenes.Add(entry.Key);
            }

            for (int i = 0; i < staleScenes.Count; i++)
                sceneContainers.Remove(staleScenes[i]);

            if (sceneContainers.TryGetValue(scene, out ServiceLocator existing)
                && UnityLifetime.IsAlive(existing)
                && existing != this)
            {
                bool thisIsSceneBootstrapper = HasSceneBootstrapper(this);
                bool existingIsSceneBootstrapper = HasSceneBootstrapper(existing);

                if (existingIsSceneBootstrapper && !thisIsSceneBootstrapper)
                    return;

                if (!existingIsSceneBootstrapper && thisIsSceneBootstrapper)
                {
                    sceneContainers[scene] = this;
                    return;
                }

                Debug.LogWarning(
                    "ServiceLocator.ConfigureForScene: Another ServiceLocator is already configured for this scene. Skipping duplicate.",
                    this);
                return;
            }

            sceneContainers[scene] = this;
        }

        static bool HasSceneBootstrapper(ServiceLocator locator) =>
            locator != null && locator.TryGetComponent(out ServiceLocatorScene _);

        internal static void RefreshForScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);

            for (int i = 0; i < roots.Count; i++)
                RefreshSceneLocatorsInHierarchy(roots[i]);

            for (int i = 0; i < roots.Count; i++)
                ReassertSceneBootstrapLocators(roots[i]);
        }

        public static bool TryGetContainerForScene(Scene scene, out ServiceLocator container)
        {
            container = null;
            if (!scene.IsValid())
                return false;

            if (!sceneContainers.TryGetValue(scene, out container)
                || !UnityLifetime.IsAlive(container))
            {
                RefreshForScene(scene);
            }

            if (sceneContainers.TryGetValue(scene, out container)
                && UnityLifetime.IsAlive(container))
            {
                return true;
            }

            return false;
        }

        public static bool TryGetForScene<T>(Scene scene, out T service) where T : class
        {
            service = null;
            if (!scene.IsValid())
                return false;

            if (!sceneContainers.TryGetValue(scene, out ServiceLocator locator)
                || !UnityLifetime.IsAlive(locator))
            {
                RefreshForScene(scene);
            }

            if (sceneContainers.TryGetValue(scene, out locator)
                && UnityLifetime.IsAlive(locator))
            {
                return locator.TryGet(out service);
            }

            return false;
        }

        static void RefreshSceneLocatorsInHierarchy(GameObject root)
        {
            if (root == null)
                return;

            if (root.TryGetComponent(out ServiceLocatorScene bootstrapper))
            {
                bootstrapper.BootstrapOnDemand();
                bootstrapper.Container?.ConfigureForScene();
            }

            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
                RefreshSceneLocatorsInHierarchy(transform.GetChild(i).gameObject);
        }

        static void ReassertSceneBootstrapLocators(GameObject root)
        {
            if (root == null)
                return;

            if (root.TryGetComponent(out ServiceLocatorScene bootstrapper))
                bootstrapper.Container?.ConfigureForScene();

            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
                ReassertSceneBootstrapLocators(transform.GetChild(i).gameObject);
        }
        
        /// <summary>
        /// Gets the global ServiceLocator instance. Creates new if none exists.
        /// </summary>        
        public static ServiceLocator Global {
            get {
                if (!UnityLifetime.IsAlive(global))
                    global = null;

                if (global != null) return global;

                if (UnityLifetime.IsApplicationQuitting || !Application.isPlaying)
                    return null;

                if (FindFirstObjectByType<ServiceLocatorGlobal>() is { } found) {
                    found.BootstrapOnDemand();
                    return global;
                }
                
                var container = new GameObject(k_globalServiceLocatorName, typeof(ServiceLocator));
                Debug.LogWarning("ServiceLocator.Global: No ServiceLocatorGlobal found in scene. Auto-creating one. Add it explicitly to avoid this.");
                container.AddComponent<ServiceLocatorGlobal>().BootstrapOnDemand();

                return global;
            }
        }
        
        /// <summary>
        /// Returns the <see cref="ServiceLocator"/> configured for the scene of a MonoBehaviour. Falls back to the global instance.
        /// </summary>
        public static ServiceLocator ForSceneOf(MonoBehaviour mb) {
            Scene scene = mb.gameObject.scene;

            if (!scene.IsValid()) return TryGetExistingGlobal();
            
            if (sceneContainers.TryGetValue(scene, out ServiceLocator container)
                && UnityLifetime.IsAlive(container)) {
                return container;
            }
            
            var rootObjects = new List<GameObject>();
            scene.GetRootGameObjects(rootObjects);

            for (int i = 0; i < rootObjects.Count; i++) {
                if (TryFindSceneBootstrapper(rootObjects[i], mb, out ServiceLocatorScene bootstrapper)) {
                    bootstrapper.BootstrapOnDemand();
                    return bootstrapper.Container;
                }
            }

            return TryGetExistingGlobal();
        }

        static bool TryFindSceneBootstrapper(GameObject root, MonoBehaviour exclude, out ServiceLocatorScene bootstrapper)
        {
            bootstrapper = null;
            if (root == null)
                return false;

            if (root.TryGetComponent(out ServiceLocatorScene candidate)
                && (Object)candidate.Container != (Object)exclude) {
                bootstrapper = candidate;
                return true;
            }

            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++) {
                if (TryFindSceneBootstrapper(transform.GetChild(i).gameObject, exclude, out bootstrapper))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the closest ServiceLocator instance to the provided 
        /// MonoBehaviour in hierarchy, the ServiceLocator for its scene, or the global ServiceLocator.
        /// </summary>
        public static ServiceLocator For(MonoBehaviour mb) {
            if (!UnityLifetime.IsAlive(mb))
                return UnityLifetime.IsApplicationQuitting ? TryGetExistingGlobal() : Global;

            ServiceLocator locator = mb.GetComponentInParent<ServiceLocator>().OrNull();
            if (locator != null)
                return locator;

            if (UnityLifetime.IsApplicationQuitting)
                return ResolveForDeregister(mb);

            return ForSceneOf(mb) ?? TryGetExistingGlobal() ?? Global;
        }

        /// <summary>
        /// Safely deregisters a service during <c>OnDestroy</c> without auto-creating locators
        /// or throwing when the locator was already destroyed.
        /// </summary>
        public static void DeregisterFor<T>(MonoBehaviour owner) where T : class {
            if (UnityLifetime.IsApplicationQuitting || !UnityLifetime.IsAlive(owner))
                return;

            ResolveForDeregister(owner)?.Deregister<T>();
        }

        /// <summary>
        /// Safely deregisters a service from the global locator during teardown.
        /// </summary>
        public static void DeregisterGlobal<T>() where T : class {
            if (UnityLifetime.IsApplicationQuitting || !UnityLifetime.IsAlive(global))
                return;

            global.Deregister<T>();
        }

        static ServiceLocator TryGetExistingGlobal() =>
            UnityLifetime.IsAlive(global) ? global : null;

        static ServiceLocator ResolveForDeregister(MonoBehaviour owner) {
            ServiceLocator locator = owner.GetComponentInParent<ServiceLocator>().OrNull();
            if (locator != null)
                return locator;

            Scene scene = owner.gameObject.scene;
            if (scene.IsValid()
                && sceneContainers.TryGetValue(scene, out ServiceLocator container)
                && UnityLifetime.IsAlive(container)) {
                return container;
            }

            return TryGetExistingGlobal();
        }
        
        /// <summary>
        /// Registers a service to the ServiceLocator using the service's type.
        /// </summary>
        /// <param name="service">The service to register.</param>  
        /// <typeparam name="T">Class type of the service to be registered.</typeparam>
        /// <returns>The ServiceLocator instance after registering the service.</returns>
        public ServiceLocator Register<T>(T service) {
            services.Register(service);
            return this;
        }
        
        /// <summary>
        /// Registers a service to the ServiceLocator using a specific type.
        /// </summary>
        /// <param name="type">The type to use for registration.</param>
        /// <param name="service">The service to register.</param>  
        /// <returns>The ServiceLocator instance after registering the service.</returns>
        public ServiceLocator Register(Type type, object service) {
            services.Register(type, service);
            return this;
        }
        
        public ServiceLocator DeregisterIfRegistered<T>() {
            services.DeregisterIfRegistered<T>();
            return this;
        }

        public ServiceLocator Deregister<T>() {
            services.Deregister<T>();
            return this;
        }
        
        public ServiceLocator Deregister(Type type) {
            services.Deregister(type);
            return this;
        }
        
        /// <summary>
        /// Gets a service of a specific type. If no service of the required type is found, an error is thrown.
        /// </summary>
        /// <param name="service">Service of type T to get.</param>  
        /// <typeparam name="T">Class type of the service to be retrieved.</typeparam>
        /// <returns>The ServiceLocator instance after attempting to retrieve the service.</returns>
        public ServiceLocator Get<T>(out T service) where T : class {
            if (TryGetService(out service)) return this;
            
            if (TryGetNextInHierarchy(out ServiceLocator container)) {
                container.Get(out service);
                return this;
            }

            throw new ArgumentException($"ServiceLocator.Get: Service of type {typeof(T).FullName} not registered");
        }

        /// <summary>
        /// Allows retrieval of a service of a specific type. An error is thrown if the required service does not exist.
        /// </summary>
        /// <typeparam name="T">Class type of the service to be retrieved.</typeparam>
        /// <returns>Instance of the service of type T.</returns>
        public T Get<T>() where T : class
        {
            T service = null;

            if (TryGetService(out service)) return service;

            if (TryGetNextInHierarchy(out ServiceLocator container))
                return container.Get<T>();

            throw new ArgumentException($"Could not resolve type '{typeof(T).FullName}'.");
        }
        
        /// <summary>
        /// Tries to get a service of a specific type. Returns whether or not the process is successful.
        /// </summary>
        /// <param name="service">Service of type T to get.</param>  
        /// <typeparam name="T">Class type of the service to be retrieved.</typeparam>
        /// <returns>True if the service retrieval was successful, false otherwise.</returns>
        public bool TryGet<T>(out T service) where T : class {
            service = null;

            if (TryGetService(out service))
                return true;

            return TryGetNextInHierarchy(out ServiceLocator container) && container.TryGet(out service);
        }        
        
        bool TryGetService<T>(out T service) where T : class {
            return services.TryGet(out service);
        }
        
        bool TryGetNextInHierarchy(out ServiceLocator container) {
            if (this == global) {
                container = null;
                return false;
            }

            container = transform.parent.OrNull()?.GetComponentInParent<ServiceLocator>().OrNull();
            if (container != null && container != this)
                return true;

            Scene scene = gameObject.scene;
            if (scene.IsValid()
                && sceneContainers.TryGetValue(scene, out ServiceLocator sceneLocator)
                && UnityLifetime.IsAlive(sceneLocator)
                && sceneLocator != this) {
                container = sceneLocator;
                return true;
            }

            container = TryGetExistingGlobal();
            return container != null;
        }
        
        void OnEnable() {
            if (this == global || !HasSceneBootstrapper(this))
                return;

            Scene scene = gameObject.scene;
            if (scene.IsValid()
                && sceneContainers.TryGetValue(scene, out ServiceLocator existing)
                && UnityLifetime.IsAlive(existing)
                && existing != this) {
                return;
            }

            ConfigureForScene();
        }

        void OnDestroy() {
            if (this == global) {
                global = null;
            } else {
                var staleScenes = new List<Scene>();
                foreach (KeyValuePair<Scene, ServiceLocator> entry in sceneContainers) {
                    if (entry.Value == this)
                        staleScenes.Add(entry.Key);
                }

                for (int i = 0; i < staleScenes.Count; i++)
                    sceneContainers.Remove(staleScenes[i]);
            }
        }
        
        // https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() {
            global = null;
            sceneContainers.Clear();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Clears persistent ServiceLocator state before Play Mode tests load isolated scenes.
        /// </summary>
        public static void ResetForPlayModeTests() {
            global = null;
            sceneContainers.Clear();

            ServiceLocator[] locators = Object.FindObjectsByType<ServiceLocator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (ServiceLocator locator in locators) {
                if (locator == null)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(locator.gameObject);
                else
                    Object.DestroyImmediate(locator.gameObject);
            }
        }
#endif

#if UNITY_EDITOR
        [MenuItem("GameObject/ServiceLocator/Add Global")]
        static void AddGlobal() {
            var go = new GameObject(k_globalServiceLocatorName, typeof(ServiceLocatorGlobal));
        }

        [MenuItem("GameObject/ServiceLocator/Add Scene")]
        static void AddScene() {
            var go = new GameObject(k_sceneServiceLocatorName, typeof(ServiceLocatorScene));
        }
#endif
    }
}