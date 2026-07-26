using System;
using UnityEngine;

namespace EnglishKingdom.LevelSystem
{
    public interface ILevelService
    {
        int Level { get; }
        bool IsReady { get; }
        event Action<int> OnLevelUp;
        event Action OnReady;
    }
}

namespace EnglishKingdom.RewardSystem
{
    [Serializable]
    public sealed class RewardBundle
    {
        [SerializeField] private int _xp;
        [SerializeField] private int _legacyCoins;

        public int Xp => _xp;
        public int LegacyCoins => _legacyCoins;

        public RewardBundle AddXp(int amount)
        {
            _xp += Mathf.Max(0, amount);
            return this;
        }

        public RewardBundle AddLegacyCoins(int amount)
        {
            _legacyCoins += Mathf.Max(0, amount);
            return this;
        }
    }

    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private RewardBundle bundle = new RewardBundle();

        public RewardBundle ToBundle()
        {
            return bundle ?? new RewardBundle();
        }
    }

    public interface IRewardService
    {
        void Grant(RewardBundle bundle, bool showPopup = true);
        void GrantLegacyCoins(int amount, bool showPopup = true);
    }
}

namespace TargetIndicators
{
    [Serializable]
    public struct TargetIndicatorId : IEquatable<TargetIndicatorId>
    {
        [SerializeField] private int _value;

        public bool IsValid => _value != 0;

        public TargetIndicatorId(int value)
        {
            _value = value;
        }

        public bool Equals(TargetIndicatorId other) => _value == other._value;
        public override bool Equals(object obj) => obj is TargetIndicatorId other && Equals(other);
        public override int GetHashCode() => _value;
        public static bool operator ==(TargetIndicatorId left, TargetIndicatorId right) => left.Equals(right);
        public static bool operator !=(TargetIndicatorId left, TargetIndicatorId right) => !left.Equals(right);
        public override string ToString() => _value.ToString();
    }

    public sealed class TargetIndicator
    {
        public TargetIndicatorId Id { get; }

        public TargetIndicator(TargetIndicatorId id)
        {
            Id = id;
        }
    }

    public sealed class TargetIndicatorManager : MonoBehaviour
    {
        int _nextId = 1;
        readonly System.Collections.Generic.Dictionary<TargetIndicatorId, Transform> _targets = new();

        public bool TryAddTarget(Transform target, out TargetIndicator indicator)
        {
            indicator = null;
            if (target == null)
                return false;

            var id = new TargetIndicatorId(_nextId++);
            _targets[id] = target;
            indicator = new TargetIndicator(id);
            return true;
        }

        public bool TryRemoveTarget(TargetIndicatorId id)
        {
            return _targets.Remove(id);
        }
    }
}
