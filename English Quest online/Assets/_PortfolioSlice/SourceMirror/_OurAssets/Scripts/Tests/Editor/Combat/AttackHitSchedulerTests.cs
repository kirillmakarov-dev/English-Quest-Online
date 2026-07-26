using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnglishKingdom.Tests.Combat
{
    [TestFixture]
    public class AttackHitSchedulerTests
    {
        private GameObject _host;
        private AttackHitScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            AttackHitScheduler.ResetTestCounters();
            _host = new GameObject("AttackHitSchedulerTestHost");
            _scheduler = _host.AddComponent<AttackHitScheduler>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
                Object.DestroyImmediate(_host);
        }

        [Test]
        public void ScheduleDelayedHit_WithZeroDelay_ResolvesImmediately()
        {
            var shape = ScriptableObject.CreateInstance<RadialAttackShapeSO>();
            shape.radius = 0.01f;

            var casterGo = new GameObject("Caster");
            Transform caster = casterGo.transform;

            int before = AttackHitScheduler.TestResolveHitCount;

            _scheduler.ScheduleDelayedHit(
                caster,
                shape,
                10f,
                10f,
                0,
                DamageType.Physical,
                Vector3.zero,
                default,
                delay: 0f);

            Assert.That(AttackHitScheduler.TestResolveHitCount, Is.EqualTo(before + 1),
                "ResolveHit should fire immediately when delay is zero.");

            Object.DestroyImmediate(casterGo);
            Object.DestroyImmediate(shape);
        }

        [UnityTest]
        public IEnumerator CancelPendingHit_BeforeDelay_DoesNotApplyDamage()
        {
            var shape = ScriptableObject.CreateInstance<RadialAttackShapeSO>();
            shape.radius = 1f;

            var casterGo = new GameObject("Caster");
            Transform caster = casterGo.transform;

            int before = AttackHitScheduler.TestResolveHitCount;

            _scheduler.ScheduleDelayedHit(
                caster,
                shape,
                10f,
                10f,
                0,
                DamageType.Physical,
                Vector3.zero,
                default,
                delay: 5f);

            _scheduler.CancelPendingHit();

            IEnumerator delayedHit = InvokeDelayedHit(
                _scheduler,
                castGeneration: 1,
                caster,
                shape,
                10f,
                10f,
                0,
                DamageType.Physical,
                Vector3.zero,
                default,
                delay: 0.05f);

            yield return RunNestedCoroutine(delayedHit);

            Assert.That(AttackHitScheduler.TestResolveHitCount, Is.EqualTo(before),
                "CancelPendingHit must prevent delayed resolve.");

            Object.DestroyImmediate(casterGo);
            Object.DestroyImmediate(shape);
        }

        [UnityTest]
        public IEnumerator ScheduleDelayedHit_WithoutCancel_ResolvesAfterDelay()
        {
            var shape = ScriptableObject.CreateInstance<RadialAttackShapeSO>();
            shape.radius = 0.01f;

            var casterGo = new GameObject("Caster");
            Transform caster = casterGo.transform;

            int before = AttackHitScheduler.TestResolveHitCount;

            // Edit-mode UnityTests do not tick MonoBehaviour coroutines started via
            // StartCoroutine, so drive DelayedHit through the test coroutine instead.
            IEnumerator delayedHit = InvokeDelayedHit(
                _scheduler,
                castGeneration: 1,
                caster,
                shape,
                10f,
                10f,
                0,
                DamageType.Physical,
                Vector3.zero,
                default,
                delay: 0.05f);

            yield return RunNestedCoroutine(delayedHit);

            Assert.That(AttackHitScheduler.TestResolveHitCount, Is.EqualTo(before + 1),
                "ResolveHit should fire once after the delay when not cancelled.");

            Object.DestroyImmediate(casterGo);
            Object.DestroyImmediate(shape);
        }

        [Test]
        public void CancelPendingHit_AfterResolve_HasNoEffect()
        {
            var shape = ScriptableObject.CreateInstance<RadialAttackShapeSO>();
            shape.radius = 0.01f;

            var casterGo = new GameObject("Caster");
            Transform caster = casterGo.transform;

            _scheduler.ScheduleDelayedHit(
                caster,
                shape,
                10f,
                10f,
                0,
                DamageType.Physical,
                Vector3.zero,
                default,
                delay: 0f);

            int after = AttackHitScheduler.TestResolveHitCount;
            Assert.That(after, Is.EqualTo(1), "Hit should resolve before testing cancel.");

            _scheduler.CancelPendingHit();

            Assert.That(AttackHitScheduler.TestResolveHitCount, Is.EqualTo(after),
                "Calling CancelPendingHit after resolve is a no-op.");

            Object.DestroyImmediate(casterGo);
            Object.DestroyImmediate(shape);
        }

        [Test]
        public void CancelPendingHit_WithNoCastScheduled_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _scheduler.CancelPendingHit(),
                "CancelPendingHit on a fresh scheduler should be safe.");
        }

        private static IEnumerator InvokeDelayedHit(
            AttackHitScheduler scheduler,
            int castGeneration,
            Transform caster,
            AttackShapeSO shape,
            float damageMin,
            float damageMax,
            int maxTargets,
            DamageType damageType,
            Vector3 originOffset,
            LayerMask targetLayers,
            float delay)
        {
            MethodInfo method = typeof(AttackHitScheduler).GetMethod(
                "DelayedHit",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(method, Is.Not.Null, "AttackHitScheduler.DelayedHit should exist.");

            return (IEnumerator)method.Invoke(
                scheduler,
                new object[] { castGeneration, caster, shape, damageMin, damageMax, maxTargets, damageType, originOffset, targetLayers, delay });
        }

        private static IEnumerator RunNestedCoroutine(IEnumerator routine)
        {
            while (routine.MoveNext())
                yield return routine.Current;
        }
    }
}
