using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace EnglishKingdom.Tests.Combat
{
    [TestFixture]
    public class AttackShapeTests
    {
        private const int CombatTargetLayer = 8;
        private static readonly LayerMask TargetMask = 1 << CombatTargetLayer;

        private readonly List<GameObject> _createdObjects = new();
        private readonly List<Object> _createdAssets = new();
        private GameObject _attacker;

        [SetUp]
        public void SetUp()
        {
            _attacker = CreateBody("Attacker", Vector3.zero, CombatTargetLayer, includeDamageReceiver: false);
            _attacker.transform.forward = Vector3.forward;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _createdObjects)
                Object.DestroyImmediate(go);

            foreach (Object asset in _createdAssets)
                Object.DestroyImmediate(asset);

            _createdObjects.Clear();
            _createdAssets.Clear();
        }

        [Test]
        public void ForwardCone_IncludesTargetInFront_ExcludesTargetBehind()
        {
            var shape = ScriptableObject.CreateInstance<ForwardConeAttackShapeSO>();
            shape.range = 8f;
            shape.coneAngle = 30f;
            _createdAssets.Add(shape);

            GameObject front = CreateBody("FrontTarget", new Vector3(0f, 0f, 4f), CombatTargetLayer);
            GameObject behind = CreateBody("BehindTarget", new Vector3(0f, 0f, -4f), CombatTargetLayer);

            var context = BuildContext(_attacker.transform, Vector3.forward);
            IReadOnlyList<Collider> hits = shape.DetectHits(context);

            Assert.That(ContainsColliderFor(hits, front), Is.True);
            Assert.That(ContainsColliderFor(hits, behind), Is.False);
        }

        [Test]
        public void Radial_HitsAllAround_ExcludesAttacker()
        {
            var shape = ScriptableObject.CreateInstance<RadialAttackShapeSO>();
            shape.radius = 4f;
            _createdAssets.Add(shape);

            GameObject north = CreateBody("North", new Vector3(0f, 0f, 3f), CombatTargetLayer);
            GameObject east = CreateBody("East", new Vector3(3f, 0f, 0f), CombatTargetLayer);

            var context = BuildContext(_attacker.transform, Vector3.forward);
            IReadOnlyList<Collider> hits = shape.DetectHits(context);

            Assert.That(ContainsColliderFor(hits, north), Is.True);
            Assert.That(ContainsColliderFor(hits, east), Is.True);
            Assert.That(hits.Count, Is.EqualTo(2));
        }

        [Test]
        public void MeleeForward_HitsCloseTargetInFront_NotFarTarget()
        {
            var shape = ScriptableObject.CreateInstance<MeleeForwardAttackShapeSO>();
            shape.range = 1.8f;
            shape.arcAngle = 90f;
            _createdAssets.Add(shape);

            GameObject close = CreateBody("Close", new Vector3(0f, 0f, 1.2f), CombatTargetLayer);
            GameObject far = CreateBody("Far", new Vector3(0f, 0f, 5f), CombatTargetLayer);

            var context = BuildContext(_attacker.transform, Vector3.forward);
            IReadOnlyList<Collider> hits = shape.DetectHits(context);

            Assert.That(ContainsColliderFor(hits, close), Is.True);
            Assert.That(ContainsColliderFor(hits, far), Is.False);
        }

        [Test]
        public void AttackExecutor_AppliesMagicDamageToDamageReceiver()
        {
            var shape = ScriptableObject.CreateInstance<MeleeForwardAttackShapeSO>();
            shape.range = 2f;
            shape.arcAngle = 180f;
            _createdAssets.Add(shape);

            GameObject target = CreateBody("Target", new Vector3(0f, 0f, 1f), CombatTargetLayer);
            var health = target.GetComponent<HealthComponent>();
            float initialHp = health.CurrentHP;

            var context = BuildContext(_attacker.transform, Vector3.forward, damageMin: 10f, damageMax: 10f);
            int applied = AttackExecutor.Execute(
                shape,
                context,
                DamageType.Magic,
                null,
                context.Origin,
                Quaternion.identity);

            Assert.AreEqual(1, applied);
            Assert.AreEqual(initialHp - 10f, health.CurrentHP);
        }

        [Test]
        public void ApplyDamage_WhenTargetInRange_AppliesDamage()
        {
            var shape = ScriptableObject.CreateInstance<MeleeForwardAttackShapeSO>();
            shape.range = 2f;
            shape.arcAngle = 180f;
            _createdAssets.Add(shape);

            GameObject target = CreateBody("Target", new Vector3(0f, 0f, 1f), CombatTargetLayer);
            var health = target.GetComponent<HealthComponent>();
            float initialHp = health.CurrentHP;

            var context = BuildContext(_attacker.transform, Vector3.forward, damageMin: 10f, damageMax: 10f);
            int applied = AttackExecutor.ApplyDamage(shape, context, DamageType.Magic);

            Assert.AreEqual(1, applied);
            Assert.AreEqual(initialHp - 10f, health.CurrentHP);
        }

        [Test]
        public void DeferredHit_TargetMovedOutBeforeImpact_NoDamage()
        {
            var shape = ScriptableObject.CreateInstance<MeleeForwardAttackShapeSO>();
            shape.range = 2f;
            shape.arcAngle = 180f;
            _createdAssets.Add(shape);

            GameObject target = CreateBody("Target", new Vector3(0f, 0f, 1f), CombatTargetLayer);
            var health = target.GetComponent<HealthComponent>();
            float initialHp = health.CurrentHP;

            target.transform.position = new Vector3(0f, 0f, 5f);
            Physics.SyncTransforms();

            var impactContext = BuildContext(_attacker.transform, Vector3.forward, damageMin: 10f, damageMax: 10f);
            int applied = AttackExecutor.ApplyDamage(shape, impactContext, DamageType.Magic);

            Assert.AreEqual(0, applied);
            Assert.AreEqual(initialHp, health.CurrentHP);
        }

        [Test]
        public void DeferredHit_TargetMovedInBeforeImpact_AppliesDamage()
        {
            var shape = ScriptableObject.CreateInstance<MeleeForwardAttackShapeSO>();
            shape.range = 2f;
            shape.arcAngle = 180f;
            _createdAssets.Add(shape);

            GameObject target = CreateBody("Target", new Vector3(0f, 0f, 5f), CombatTargetLayer);
            var health = target.GetComponent<HealthComponent>();
            float initialHp = health.CurrentHP;

            target.transform.position = new Vector3(0f, 0f, 1f);
            Physics.SyncTransforms();

            var impactContext = BuildContext(_attacker.transform, Vector3.forward, damageMin: 10f, damageMax: 10f);
            int applied = AttackExecutor.ApplyDamage(shape, impactContext, DamageType.Magic);

            Assert.AreEqual(1, applied);
            Assert.AreEqual(initialHp - 10f, health.CurrentHP);
        }

        [Test]
        public void DeferredHit_TargetStaysInRange_AppliesDamageOnce()
        {
            var shape = ScriptableObject.CreateInstance<MeleeForwardAttackShapeSO>();
            shape.range = 2f;
            shape.arcAngle = 180f;
            _createdAssets.Add(shape);

            GameObject target = CreateBody("Target", new Vector3(0f, 0f, 1f), CombatTargetLayer);
            var health = target.GetComponent<HealthComponent>();
            float initialHp = health.CurrentHP;

            var impactContext = BuildContext(_attacker.transform, Vector3.forward, damageMin: 10f, damageMax: 10f);
            int applied = AttackExecutor.ApplyDamage(shape, impactContext, DamageType.Magic);

            Assert.AreEqual(1, applied);
            Assert.AreEqual(initialHp - 10f, health.CurrentHP);
        }

        [Test]
        public void ApplyDamage_MaxTargets_LimitsUniqueHits()
        {
            var shape = ScriptableObject.CreateInstance<RadialAttackShapeSO>();
            shape.radius = 4f;
            _createdAssets.Add(shape);

            var targets = new GameObject[5];
            var healthComponents = new HealthComponent[5];
            for (int i = 0; i < targets.Length; i++)
            {
                float angle = i * (360f / targets.Length) * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Sin(angle) * 2f, 0f, Mathf.Cos(angle) * 2f);
                targets[i] = CreateBody($"Target{i}", position, CombatTargetLayer);
                healthComponents[i] = targets[i].GetComponent<HealthComponent>();
            }

            var context = BuildContext(_attacker.transform, Vector3.forward, damageMin: 10f, damageMax: 10f, maxTargets: 2);
            int applied = AttackExecutor.ApplyDamage(shape, context, DamageType.Magic);

            Assert.AreEqual(2, applied);

            int damagedCount = 0;
            foreach (HealthComponent health in healthComponents)
            {
                if (health.CurrentHP < 100f)
                    damagedCount++;
            }

            Assert.AreEqual(2, damagedCount);
        }

        [Test]
        public void ApplyDamage_RandomDamage_PerTargetRoll()
        {
            var shape = ScriptableObject.CreateInstance<MeleeForwardAttackShapeSO>();
            shape.range = 2f;
            shape.arcAngle = 180f;
            _createdAssets.Add(shape);

            GameObject target = CreateBody("Target", new Vector3(0f, 0f, 1f), CombatTargetLayer);
            var health = target.GetComponent<HealthComponent>();

            var damageValues = new HashSet<float>();
            for (int i = 0; i < 50; i++)
            {
                health.SetHP(health.MaxHP);
                var context = BuildContext(_attacker.transform, Vector3.forward, damageMin: 5f, damageMax: 10f);
                AttackExecutor.ApplyDamage(shape, context, DamageType.Magic);

                float damageTaken = 100f - health.CurrentHP;
                Assert.That(damageTaken, Is.InRange(5f, 10f));
                damageValues.Add(damageTaken);
            }

            Assert.That(damageValues.Count, Is.GreaterThan(1),
                "Random damage should produce more than one distinct value over many rolls.");
        }

        private AttackHitContext BuildContext(
            Transform attacker,
            Vector3 forward,
            float damageMin = 0f,
            float damageMax = 0f,
            int maxTargets = 0)
        {
            return new AttackHitContext(
                attacker.position,
                forward,
                attacker,
                TargetMask,
                attacker.gameObject,
                damageMin,
                damageMax,
                maxTargets);
        }

        private GameObject CreateBody(string name, Vector3 position, int layer, bool includeDamageReceiver = true)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.position = position;

            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            var stats = ScriptableObject.CreateInstance<HealthStatsSO>();
            stats.maxHP = 100f;
            stats.invincibilityDurationOnHit = 0f;
            _createdAssets.Add(stats);

            var health = go.AddComponent<HealthComponent>();
            typeof(HealthComponent)
                .GetField("_stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(health, stats);
            typeof(HealthComponent)
                .GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(health, null);

            if (includeDamageReceiver)
                go.AddComponent<DamageReceiver>();

            _createdObjects.Add(go);
            Physics.SyncTransforms();
            return go;
        }

        private static bool ContainsColliderFor(IReadOnlyList<Collider> hits, GameObject target)
        {
            Collider targetCollider = target.GetComponent<Collider>();
            foreach (Collider hit in hits)
            {
                if (hit == targetCollider)
                    return true;
            }

            return false;
        }
    }
}
