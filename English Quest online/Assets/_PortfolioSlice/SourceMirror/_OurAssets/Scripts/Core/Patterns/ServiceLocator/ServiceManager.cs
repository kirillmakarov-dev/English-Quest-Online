using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator {
    public class ServiceManager {
        readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        public IEnumerable<object> RegisteredServices => services.Values;
        
        public bool TryGet<T>(out T service) where T : class {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object obj)) {
                if (!IsServiceAlive(obj)) {
                    services.Remove(type);
                    service = null;
                    return false;
                }

                service = obj as T;
                return service != null;
            }

            service = null;
            return false;
        }

        public T Get<T>() where T : class {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object obj)) {
                if (!IsServiceAlive(obj))
                {
                    services.Remove(type);
                    throw new ArgumentException($"ServiceManager.Get: Service of type {type.FullName} not registered");
                }

                T service = obj as T;
                if (service == null)
                {
                    services.Remove(type);
                    throw new ArgumentException($"ServiceManager.Get: Service of type {type.FullName} is registered but not assignable");
                }

                return service;
            }
            
            throw new ArgumentException($"ServiceManager.Get: Service of type {type.FullName} not registered");
        }

        public ServiceManager Register<T>(T service) {
            Type type = typeof(T);

            if (services.TryGetValue(type, out object existing)) {
                if (ReferenceEquals(existing, service))
                    return this;

                if (!IsServiceAlive(existing)) {
                    services[type] = service;
                    return this;
                }

                Debug.LogWarning($"ServiceManager.Register: Service of type {type.FullName} already registered");
                return this;
            }

            services.Add(type, service);
            return this;
        }

        public ServiceManager Register(Type type, object service) {
            if (!type.IsInstanceOfType(service)) {
                throw new ArgumentException("Type of service does not match type of service interface", nameof(service));
            }

            if (services.TryGetValue(type, out object existing)) {
                if (ReferenceEquals(existing, service))
                    return this;

                if (!IsServiceAlive(existing)) {
                    services[type] = service;
                    return this;
                }

                Debug.LogWarning($"ServiceManager.Register: Service of type {type.FullName} already registered");
                return this;
            }

            services.Add(type, service);
            return this;
        }

        public bool TryDeregister<T>() => services.Remove(typeof(T));

        public ServiceManager DeregisterIfRegistered<T>() {
            services.Remove(typeof(T));
            return this;
        }

        public ServiceManager Deregister<T>() {
            Type type = typeof(T);
            if (!services.Remove(type) && !UnityLifetime.IsApplicationQuitting) {
                Debug.LogWarning($"ServiceManager.Deregister: Service of type {type.FullName} not registered");
            }
            return this;
        }

        public ServiceManager Deregister(Type type) {
            if (!services.Remove(type) && !UnityLifetime.IsApplicationQuitting) {
                Debug.LogWarning($"ServiceManager.Deregister: Service of type {type.FullName} not registered");
            }
            return this;
        }

        static bool IsServiceAlive(object service) => service != null && (service is not Object unityObj || unityObj);
    }
}
