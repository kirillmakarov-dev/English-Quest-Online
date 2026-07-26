using System.Reflection;
using Cysharp.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace EnglishKingdom.Tests.Social
{
    [TestFixture]
    public class PartyInviteServiceEditTests
    {
        private PartyInviteService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new PartyInviteService();
            _service.Bind(new StubSessionService(), new StubPartyService());
        }

        [Test]
        public void DeclineInvite_ClearsPendingInvite()
        {
            SetPendingInvite(new PartyInvite(default, "Leader"));

            _service.DeclineInvite();

            Assert.That(_service.PendingInvite, Is.Null);
        }

        [Test]
        public void AcceptInvite_WithNoPendingInvite_IsNoOp()
        {
            Assert.That(_service.PendingInvite, Is.Null);

            Assert.DoesNotThrow(() => _service.AcceptInvite());

            Assert.That(_service.PendingInvite, Is.Null);
        }

        [Test]
        public void DeclineInvite_FiresInviteStateChanged()
        {
            SetPendingInvite(new PartyInvite(default, "Leader"));
            int changeCount = 0;
            _service.OnInviteStateChanged += () => changeCount++;

            _service.DeclineInvite();

            Assert.That(changeCount, Is.EqualTo(1));
        }

        private void SetPendingInvite(PartyInvite invite)
        {
            FieldInfo field = typeof(PartyInviteService).GetField(
                "_pendingInvite",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_service, invite);
        }

        private sealed class StubSessionService : INetworkSessionService
        {
            public NetworkRunner Runner => null;

            public UniTask StartSharedSession(string sessionName, int sceneBuildIndex) =>
                UniTask.CompletedTask;

            public UniTask StartSharedSession(string sessionName, int sceneBuildIndex, int maxPlayers, bool enableClientSessionCreation = true) =>
                UniTask.CompletedTask;

            public UniTask StartSharedSession(NetworkSessionProfile profile) =>
                UniTask.CompletedTask;

            public UniTask JoinSharedSession(string sessionName) => UniTask.CompletedTask;

            public UniTask JoinOpenWorldAsync(string openWorldSceneName) => UniTask.CompletedTask;

            public UniTask JoinOpenWorldAsync(NetworkSessionProfile profile) => UniTask.CompletedTask;

            public UniTask Disconnect() => UniTask.CompletedTask;
        }

        private sealed class StubPartyService : IPartyService
        {
            public event System.Action OnPartyChanged;

            public PartySnapshot GetMyParty() => default;
            public bool CanInvite(PlayerRef target) => false;
            public void LeaveParty() { }
            public void Kick(PlayerRef target) { }
            public int GetPartySize(PlayerRef leaderRef) => 0;
            public PlayerRef GetPartyLeader(PlayerRef member) => PlayerRef.None;
            public void NotifyChanged() => OnPartyChanged?.Invoke();
        }
    }
}
