using System;
using System.Collections.Generic;
using System.Reflection;
using Fusion;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    public sealed class PortfolioPlayerLockService : MonoBehaviour, IPlayerLockSystem
    {
        [SerializeField] private PortfolioDemoPlayerController playerController;
        [SerializeField] private MonoBehaviour starterController;
        [SerializeField] private MonoBehaviour starterInputs;
        [SerializeField] private PlayerInteraction playerInteraction;

        private readonly Dictionary<PlayerLockSystem.LockType, HashSet<object>> locks = new();
        private bool isLocalPlayer = true;
        private bool isRegistered;

        private void Awake()
        {
            foreach (PlayerLockSystem.LockType type in System.Enum.GetValues(typeof(PlayerLockSystem.LockType)))
                locks[type] = new HashSet<object>();

            if (playerController == null)
                playerController = GetComponent<PortfolioDemoPlayerController>();

            if (starterController == null)
                starterController = FindBehaviourByTypeName("ThirdPersonController");

            if (starterInputs == null)
                starterInputs = FindBehaviourByTypeName("StarterAssetsInputs");

            if (playerInteraction == null)
                playerInteraction = GetComponent<PlayerInteraction>();

        }

        private void Start()
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsValid)
                isLocalPlayer = NetworkPlayerOwnership.IsLocal(networkObject);

            RefreshRegistration();
        }

        private void OnDestroy()
        {
            if (isRegistered)
                ServiceLocator.DeregisterFor<IPlayerLockSystem>(this);

            if (isLocalPlayer)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void SetLocalPlayer(bool value)
        {
            isLocalPlayer = value;
            RefreshRegistration();
        }

        private void RefreshRegistration()
        {
            if (isLocalPlayer && !isRegistered)
            {
                ServiceLocator.For(this).Register<IPlayerLockSystem>(this);
                isRegistered = true;
            }
            else if (!isLocalPlayer && isRegistered)
            {
                ServiceLocator.DeregisterFor<IPlayerLockSystem>(this);
                isRegistered = false;
            }
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
                    if (starterController != null)
                        starterController.enabled = !isLocked;
                    else
                        playerController?.SetMovementLocked(isLocked);
                    break;
                case PlayerLockSystem.LockType.Interaction:
                    if (playerInteraction != null)
                        playerInteraction.enabled = !isLocked;
                    else
                        playerController?.SetInteractionLocked(isLocked);
                    break;
                case PlayerLockSystem.LockType.GameplayInput:
                    if (playerInteraction != null)
                        playerInteraction.enabled = !isLocked;
                    else
                        playerController?.SetInteractionLocked(isLocked);
                    break;
                case PlayerLockSystem.LockType.Cursor:
                    if (starterInputs != null)
                    {
                        SetBoolMember(starterInputs, "cursorLocked", !isLocked);
                        SetBoolMember(starterInputs, "cursorInputForLook", !isLocked);
                    }

                    Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
                    Cursor.visible = isLocked;
                    break;
                case PlayerLockSystem.LockType.Camera:
                    if (starterController != null)
                        SetBoolMember(starterController, "LockCameraPosition", isLocked);
                    break;
            }
        }

        private MonoBehaviour FindBehaviourByTypeName(string typeName)
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == typeName)
                    return behaviour;
            }

            return null;
        }

        private static void SetBoolMember(MonoBehaviour target, string memberName, bool value)
        {
            if (target == null)
                return;

            Type targetType = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            PropertyInfo property = targetType.GetProperty(memberName, flags);
            if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
            {
                property.SetValue(target, value);
                return;
            }

            FieldInfo field = targetType.GetField(memberName, flags);
            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(target, value);
        }
    }
}

