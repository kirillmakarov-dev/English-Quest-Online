using NUnit.Framework;
using UnityEngine;

namespace EnglishQuest.Tests.Social
{
    [TestFixture]
    public class PartyTravelAuthorityTests
    {
        private readonly PartyTravelAuthority _authority = new();
        private GameObject _interactorObject;

        [TearDown]
        public void TearDown()
        {
            if (_interactorObject != null)
            {
                Object.DestroyImmediate(_interactorObject);
                _interactorObject = null;
            }
        }

        [Test]
        public void CanInitiateTravel_NullInteractor_ReturnsFalse()
        {
            Assert.That(_authority.CanInitiateTravel(null), Is.False);
        }

        [Test]
        public void CanInitiateTravel_UnspawnedInteractor_ReturnsTrue()
        {
            _interactorObject = new GameObject("Interactor");
            var interactor = _interactorObject.AddComponent<PlayerInteraction>();

            Assert.That(interactor.Object, Is.Null);
            Assert.That(_authority.CanInitiateTravel(interactor), Is.True);
        }
    }
}

