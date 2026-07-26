using System.Collections.Generic;
using NUnit.Framework;
using EnglishKingdom.UI.HUD;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnglishKingdom.Tests.UI
{
    [TestFixture]
    public class EntityHealthBarPresenterTests
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
        public void EntityHealthBarPresenter_HiddenUntilDamaged_ThenHidesOnDeath()
        {
            HealthComponent health = CreateHealth(maxHp: 100f);
            CanvasGroup canvasGroup = CreateBarPresenter(health);

            Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

            health.TakeDamage(10f, new DamageInfo(10f, DamageType.Physical));

            Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

            health.TakeDamage(100f, new DamageInfo(100f, DamageType.Physical));
            health.NotifyDied(DeathContext.None);

            Assert.That(canvasGroup.alpha, Is.EqualTo(0f));
        }

        private HealthComponent CreateHealth(float maxHp)
        {
            var stats = ScriptableObject.CreateInstance<HealthStatsSO>();
            stats.maxHP = maxHp;
            stats.invincibilityDurationOnHit = 0f;
            _createdObjects.Add(stats);

            var monster = new GameObject("Monster");
            _createdObjects.Add(monster);

            var health = monster.AddComponent<HealthComponent>();
            typeof(HealthComponent)
                .GetField("_stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(health, stats);
            typeof(HealthComponent)
                .GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(health, null);

            return health;
        }

        private static void InvokeLifecycleMethod(MonoBehaviour component, string methodName)
        {
            component.GetType()
                .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(component, null);
        }

        private CanvasGroup CreateBarPresenter(HealthComponent health)
        {
            var bar = new GameObject("MonsterHealthBar");
            bar.transform.SetParent(health.transform, false);
            _createdObjects.Add(bar);

            var canvasGroup = bar.AddComponent<CanvasGroup>();
            var presenter = bar.AddComponent<EntityHealthBarPresenter>();

            InvokeLifecycleMethod(presenter, "Awake");
            InvokeLifecycleMethod(presenter, "OnEnable");

            return canvasGroup;
        }
    }
}
