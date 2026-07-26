using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.ServiceLocatorLifecycle
{
    [TestFixture]
    public class ServiceManagerLifecycleTests
    {
        sealed class TestMonoService : MonoBehaviour { }

        sealed class OtherMonoService : MonoBehaviour { }

        [Test]
        public void TryGet_RemovesDestroyedUnityService()
        {
            var manager = new ServiceManager();
            var go = new GameObject("TestMonoService");
            var service = go.AddComponent<TestMonoService>();

            manager.Register<TestMonoService>(service);
            Assert.That(manager.TryGet(out TestMonoService resolved), Is.True);
            Assert.That(resolved, Is.SameAs(service));

            Object.DestroyImmediate(go);

            Assert.That(manager.TryGet(out TestMonoService afterDestroy), Is.False);
            Assert.That(afterDestroy, Is.Null);
        }

        [Test]
        public void Get_Throws_WhenRegisteredTypeDoesNotMatch()
        {
            var manager = new ServiceManager();
            var go = new GameObject("OtherMonoService");
            var service = go.AddComponent<OtherMonoService>();

            manager.Register<OtherMonoService>(service);

            Assert.Throws<System.ArgumentException>(() => manager.Get<TestMonoService>());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void DeregisterFor_DoesNotThrow_WhenNoLocatorExists()
        {
            var go = new GameObject("DeregisterOwner");
            var owner = go.AddComponent<TestMonoService>();

            Assert.DoesNotThrow(() => ServiceLocator.DeregisterFor<TestMonoService>(owner));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void DeregisterFor_DoesNotThrow_AfterLocatorDestroyed()
        {
            var locatorGo = new GameObject("Locator", typeof(ServiceLocator));
            var ownerGo = new GameObject("Owner");
            var owner = ownerGo.AddComponent<TestMonoService>();
            var locator = locatorGo.GetComponent<ServiceLocator>();

            locator.Register<TestMonoService>(owner);
            Object.DestroyImmediate(locatorGo);

            Assert.DoesNotThrow(() => ServiceLocator.DeregisterFor<TestMonoService>(owner));

            Object.DestroyImmediate(ownerGo);
        }

        [Test]
        public void TryGet_ReturnsFalse_WhenRegisteredServiceIsWrongType()
        {
            var manager = new ServiceManager();
            var go = new GameObject("OtherMonoService");
            var service = go.AddComponent<OtherMonoService>();

            manager.Register<OtherMonoService>(service);
            Assert.That(manager.TryGet(out TestMonoService resolved), Is.False);
            Assert.That(resolved, Is.Null);

            Object.DestroyImmediate(go);
        }
    }
}
