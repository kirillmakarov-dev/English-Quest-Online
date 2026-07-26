using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnglishKingdom.Tests.Combat
{
    [TestFixture]
    public class DamageAttributionComponentTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _createdObjects)
                Object.DestroyImmediate(obj);

            _createdObjects.Clear();
        }

        [Test]
        public void RecordInstigator_OverwritesLastInstigator()
        {
            var root = CreateObject("Victim");
            root.AddComponent<HealthComponent>();
            var attribution = root.AddComponent<DamageAttributionComponent>();

            var first = CreateObject("FirstAttacker");
            var second = CreateObject("SecondAttacker");

            attribution.RecordInstigator(first);
            attribution.RecordInstigator(second);

            Assert.That(attribution.LastInstigator, Is.SameAs(second));
            Assert.That(attribution.BuildDeathContext().Killer, Is.SameAs(second));
        }

        [Test]
        public void Clear_RemovesLastInstigator()
        {
            var root = CreateObject("Victim");
            root.AddComponent<HealthComponent>();
            var attribution = root.AddComponent<DamageAttributionComponent>();
            attribution.RecordInstigator(CreateObject("Attacker"));

            attribution.Clear();

            Assert.That(attribution.LastInstigator, Is.Null);
            Assert.That(attribution.BuildDeathContext().HasKiller, Is.False);
        }

        [Test]
        public void RecordInstigator_IgnoresNullSource()
        {
            var root = CreateObject("Victim");
            root.AddComponent<HealthComponent>();
            var attribution = root.AddComponent<DamageAttributionComponent>();
            var attacker = CreateObject("Attacker");

            attribution.RecordInstigator(attacker);
            attribution.RecordInstigator(null);

            Assert.That(attribution.LastInstigator, Is.SameAs(attacker));
        }

        private GameObject CreateObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }
    }
}
