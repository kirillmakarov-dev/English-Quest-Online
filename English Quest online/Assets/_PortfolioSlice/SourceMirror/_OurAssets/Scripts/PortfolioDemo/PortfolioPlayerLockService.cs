using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.PortfolioDemo
{
    public sealed class PortfolioPlayerLockService : MonoBehaviour, IPlayerLockSystem
    {
        [SerializeField] private PortfolioDemoPlayerController playerController;

        private readonly Dictionary<PlayerLockSystem.LockType, HashSet<object>> locks = new();

        private void Awake()
        {
            foreach (PlayerLockSystem.LockType type in System.Enum.GetValues(typeof(PlayerLockSystem.LockType)))
                locks[type] = new HashSet<object>();

            if (playerController == null)
                playerController = GetComponent<PortfolioDemoPlayerController>();

            ServiceLocator.For(this).Register<IPlayerLockSystem>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.DeregisterFor<IPlayerLockSystem>(this);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Lock(PlayerLockSystem.LockType type, object source)
        {
            if (source != null && locks[type].Add(source))
                ApplyState(type);
        }

        public void Unlock(PlayerLockSystem.LockType type, object source)
        {
            if (source != null && locks[type].Remove(source))
                ApplyState(type);
        }

        public void Lock(object source, params PlayerLockSystem.LockType[] types)
        {
            foreach (PlayerLockSystem.LockType type in types)
                Lock(type, source);
        }

        public void Unlock(object source, params PlayerLockSystem.LockType[] types)
        {
            foreach (PlayerLockSystem.LockType type in types)
                Unlock(type, source);
        }

        public bool IsLocked(PlayerLockSystem.LockType type) => locks[type].Count > 0;

        public bool IsLockedByOther(PlayerLockSystem.LockType type, object self)
        {
            return locks[type].Count > 0 && !locks[type].Contains(self);
        }

        private void ApplyState(PlayerLockSystem.LockType type)
        {
            bool isLocked = IsLocked(type);
            switch (type)
            {
                case PlayerLockSystem.LockType.Movement:
                    playerController?.SetMovementLocked(isLocked);
                    break;
                case PlayerLockSystem.LockType.Interaction:
                case PlayerLockSystem.LockType.GameplayInput:
                    playerController?.SetInteractionLocked(isLocked);
                    break;
                case PlayerLockSystem.LockType.Cursor:
                    Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
                    Cursor.visible = isLocked;
                    break;
            }
        }
    }
}
