using System;
using System.Reflection;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace EnglishKingdom.Tests.Bike
{
    // =========================================================================
    // Shared test doubles
    // =========================================================================

    /// <summary>
    /// Spy implementation of <see cref="IInteractionUI"/>.
    /// Tracks whether the panel is considered visible and how many times each
    /// method was called.
    /// </summary>
    internal sealed class SpyInteractionUI : IInteractionUI
    {
        public bool  IsVisible   { get; set; }
        public int   ShowCalls   { get; private set; }
        public int   HideCalls   { get; private set; }
        public string LastMessage { get; private set; }

        public void Show(string message, string button = "E")
        {
            IsVisible   = true;
            LastMessage = message;
            ShowCalls++;
        }

        public void Hide()
        {
            IsVisible = false;
            HideCalls++;
        }
    }

    /// <summary>
    /// Stub implementation of <see cref="IBikeMount"/> that exposes simple
    /// settable properties so tests can drive any scenario without Fusion.
    /// </summary>
    internal sealed class StubBikeMount : IBikeMount
    {
        public PlayerRef RidingPlayer      { get; set; } = PlayerRef.None;
        public bool      IsLocalPlayerRiding { get; set; }
        public bool      IsOccupied          { get; set; }

        public event Action<PlayerRef, PlayerRef> OnRiderChanged;
        public event Action OnLocalMounted;
        public event Action OnLocalDismounted;

        public void Mount()    => IsOccupied = true;
        public void Dismount() => IsOccupied = false;

        // Helpers to fire events from tests.
        public void RaiseLocalMounted()    => OnLocalMounted?.Invoke();
        public void RaiseLocalDismounted() => OnLocalDismounted?.Invoke();
        public void RaiseRiderChanged(PlayerRef prev, PlayerRef cur) => OnRiderChanged?.Invoke(prev, cur);
    }

    // =========================================================================
    // PlayerFormSwitcher — name tag visibility
    // =========================================================================

    /// <summary>
    /// Verifies that <see cref="PlayerFormSwitcher.SetVisualsVisible"/> correctly
    /// toggles the name-tag root alongside the animal rig.
    /// These are Edit Mode tests — no Fusion runner required.
    /// </summary>
    [TestFixture]
    public class PlayerFormSwitcherNameTagTests
    {
        private static readonly FieldInfo NameTagRootField =
            typeof(PlayerFormSwitcher).GetField(
                "_nameTagRoot",
                BindingFlags.NonPublic | BindingFlags.Instance);

        private GameObject        _go;
        private PlayerFormSwitcher _switcher;
        private GameObject        _nameTagRoot;

        [SetUp]
        public void SetUp()
        {
            _go      = new GameObject("PlayerFormSwitcher_Test");
            _go.SetActive(false);  // Prevent Spawned() from firing (no Fusion runner).
            _switcher = _go.AddComponent<PlayerFormSwitcher>();

            _nameTagRoot = new GameObject("NameTagRoot");
            _nameTagRoot.transform.SetParent(_go.transform);

            // Inject _nameTagRoot via reflection — avoids exposing the field.
            NameTagRootField.SetValue(_switcher, _nameTagRoot);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void VisualsByDefault_AreVisible()
        {
            Assert.IsTrue(_switcher.AreVisualsVisible);
        }

        [Test]
        public void SetVisualsVisible_False_HidesNameTagRoot()
        {
            _switcher.SetVisualsVisible(false);

            Assert.IsFalse(_nameTagRoot.activeSelf,
                "Name tag root should be deactivated when visuals are hidden.");
        }

        [Test]
        public void SetVisualsVisible_True_AfterHide_ShowsNameTagRoot()
        {
            _switcher.SetVisualsVisible(false);
            _switcher.SetVisualsVisible(true);

            Assert.IsTrue(_nameTagRoot.activeSelf,
                "Name tag root should be re-activated when visuals are restored.");
        }

        [Test]
        public void SetVisualsVisible_False_LeavesAreVisualsVisibleFalse()
        {
            _switcher.SetVisualsVisible(false);

            Assert.IsFalse(_switcher.AreVisualsVisible);
        }
    }
    // =========================================================================
    // BikeEntrySystem — interaction prompt and CanInteract logic
    // =========================================================================

    /// <summary>
    /// Verifies <see cref="BikeEntrySystem.InteractionPrompt"/> and
    /// <see cref="BikeEntrySystem.CanInteract"/> using a <see cref="StubBikeMount"/>.
    /// No Fusion session or scene setup required — the null-Object guard in
    /// CanInteract allows these to run as pure Edit Mode tests.
    /// </summary>
    [TestFixture]
    public class BikeEntrySystemTests
    {
        private GameObject      _go;
        private BikeEntrySystem _system;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("BikeEntrySystem_Test");
            _go.SetActive(false);  // Prevent Spawned() from running.
            _system = _go.AddComponent<BikeEntrySystem>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
        }

        // ----- InteractionPrompt ------------------------------------------------

        [Test]
        public void InteractionPrompt_WhenNotRiding_ReturnsRide()
        {
            _system.SetBikeMountForTest(new StubBikeMount { IsLocalPlayerRiding = false });

            Assert.AreEqual("Ride", _system.InteractionPrompt);
        }

        [Test]
        public void InteractionPrompt_WhenRiding_ReturnsExitBike()
        {
            _system.SetBikeMountForTest(new StubBikeMount { IsLocalPlayerRiding = true });

            Assert.AreEqual("Exit bike", _system.InteractionPrompt);
        }

        // ----- CanInteract ------------------------------------------------------

        [Test]
        public void CanInteract_WithNullMount_ReturnsFalse()
        {
            // No mount injected — _mount is null.
            Assert.IsFalse(_system.CanInteract);
        }

        [Test]
        public void CanInteract_WhenUnoccupied_ReturnsTrue()
        {
            _system.SetBikeMountForTest(new StubBikeMount { IsOccupied = false });

            Assert.IsTrue(_system.CanInteract,
                "An empty bike should be interactable.");
        }

        [Test]
        public void CanInteract_WhenOccupiedByOtherPlayer_ReturnsFalse()
        {
            _system.SetBikeMountForTest(new StubBikeMount
            {
                IsOccupied           = true,
                IsLocalPlayerRiding  = false,
            });

            Assert.IsFalse(_system.CanInteract,
                "A bike occupied by another player should not be interactable.");
        }

        [Test]
        public void CanInteract_WhenLocalPlayerIsRider_ReturnsTrue()
        {
            _system.SetBikeMountForTest(new StubBikeMount
            {
                IsOccupied           = true,
                IsLocalPlayerRiding  = true,  // local player is the rider → can exit
            });

            Assert.IsTrue(_system.CanInteract,
                "The local player riding the bike should be able to interact (to exit).");
        }
    }
}
