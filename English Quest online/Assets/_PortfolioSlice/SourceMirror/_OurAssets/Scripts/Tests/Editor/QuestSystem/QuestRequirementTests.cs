using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.QuestSystem
{
    /// <summary>
    /// Unit tests for <see cref="QuestRequirement.IsMet"/>.
    /// Each test creates the minimal <see cref="QuestInfo"/> GameObjects it needs
    /// and tears them down immediately after.
    /// </summary>
    [TestFixture]
    public class QuestRequirementTests
    {
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private QuestInfo CreateQuestWithState(QuestState state)
        {
            var go = new GameObject("TestQuest");
            _created.Add(go);
            var qi = go.AddComponent<QuestInfo>();
            qi.questSteps = new List<QuestStep>();
            qi.requirements = new List<QuestRequirement>();
            qi.SetState(state);
            return qi;
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Test]
        public void IsMet_NullRequiredQuestsList_ReturnsTrue()
        {
            var req = new QuestRequirement { requiredQuests = null };

            Assert.IsTrue(req.IsMet());
        }

        [Test]
        public void IsMet_EmptyRequiredQuestsList_ReturnsTrue()
        {
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo>() };

            Assert.IsTrue(req.IsMet());
        }

        [Test]
        public void IsMet_SingleFinishedQuest_ReturnsTrue()
        {
            var quest = CreateQuestWithState(QuestState.FINISHED);
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { quest } };

            Assert.IsTrue(req.IsMet());
        }

        [Test]
        public void IsMet_SingleQuestInProgress_ReturnsFalse()
        {
            var quest = CreateQuestWithState(QuestState.IN_PROGRESS);
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { quest } };

            Assert.IsFalse(req.IsMet());
        }

        [Test]
        public void IsMet_SingleQuestCanStart_ReturnsFalse()
        {
            var quest = CreateQuestWithState(QuestState.CAN_START);
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { quest } };

            Assert.IsFalse(req.IsMet());
        }

        [Test]
        public void IsMet_AllRequiredQuestsFinished_ReturnsTrue()
        {
            var q1 = CreateQuestWithState(QuestState.FINISHED);
            var q2 = CreateQuestWithState(QuestState.FINISHED);
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { q1, q2 } };

            Assert.IsTrue(req.IsMet());
        }

        [Test]
        public void IsMet_OneRequiredQuestNotFinished_ReturnsFalse()
        {
            var q1 = CreateQuestWithState(QuestState.FINISHED);
            var q2 = CreateQuestWithState(QuestState.CAN_START);
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { q1, q2 } };

            Assert.IsFalse(req.IsMet());
        }

        [Test]
        public void IsMet_NullQuestEntryInList_ReturnsFalse()
        {
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { null } };

            Assert.IsFalse(req.IsMet());
        }

        [Test]
        public void IsMet_NullAndFinishedMixed_ReturnsFalse()
        {
            var q1 = CreateQuestWithState(QuestState.FINISHED);
            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { q1, null } };

            Assert.IsFalse(req.IsMet());
        }

        [Test]
        public void IsMet_PlayerLevelBelowMinimum_ReturnsFalse()
        {
            var req = new QuestRequirement { minPlayerLevel = 5 };
            var levelProvider = new StubPlayerLevelProvider { Level = 3 };

            Assert.IsFalse(req.IsMet(levelProvider: levelProvider));
        }

        [Test]
        public void IsMet_PlayerLevelAtMinimum_ReturnsTrue()
        {
            var req = new QuestRequirement { minPlayerLevel = 5 };
            var levelProvider = new StubPlayerLevelProvider { Level = 5 };

            Assert.IsTrue(req.IsMet(levelProvider: levelProvider));
        }

        [Test]
        public void IsMet_RequiredQuestIdFinished_ReturnsTrue()
        {
            var questService = new StubQuestService();
            QuestInfo prereq = CreateQuestWithState(QuestState.FINISHED);
            prereq.id = "q01";
            questService.RegisterQuest(prereq);

            var req = new QuestRequirement { requiredQuestIds = new List<string> { "q01" } };

            Assert.IsTrue(req.IsMet(questService));
        }

        [Test]
        public void IsMet_RequiredQuestIdNotFinished_ReturnsFalse()
        {
            var questService = new StubQuestService();
            QuestInfo prereq = CreateQuestWithState(QuestState.IN_PROGRESS);
            prereq.id = "q01";
            questService.RegisterQuest(prereq);

            var req = new QuestRequirement { requiredQuestIds = new List<string> { "q01" } };

            Assert.IsFalse(req.IsMet(questService));
        }

        [Test]
        public void IsMet_RequiredQuestIdsWithoutService_ReturnsFalse()
        {
            var req = new QuestRequirement { requiredQuestIds = new List<string> { "q01" } };

            Assert.IsFalse(req.IsMet());
        }
    }
}
