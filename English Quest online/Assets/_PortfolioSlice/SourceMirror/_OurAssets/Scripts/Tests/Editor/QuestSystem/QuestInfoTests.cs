using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.QuestSystem
{
    /// <summary>
    /// Edit-mode tests for <see cref="QuestInfo"/>.
    ///
    /// In Edit Mode, Unity does NOT invoke Awake when AddComponent is called,
    /// so we call <see cref="QuestInfo.InitializeQuest"/> explicitly where required.
    /// </summary>
    [TestFixture]
    public class QuestInfoTests
    {
        private List<GameObject> _created;
        private LogLevel _previousLogLevel;

        [SetUp]
        public void SetUp()
        {
            _previousLogLevel = AppLog.Level;
            AppLog.Level = LogLevel.Warning;

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

            AppLog.Level = _previousLogLevel;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private QuestInfo CreateQuestInfo(int stepCount = 0, List<QuestRequirement> requirements = null)
        {
            var go = new GameObject("TestQuestInfo");
            _created.Add(go);
            var qi = go.AddComponent<QuestInfo>();
            qi.questSteps = new List<QuestStep>();
            qi.requirements = requirements ?? new List<QuestRequirement>();

            for (int i = 0; i < stepCount; i++)
            {
                var stepGo = new GameObject($"Step_{i}");
                _created.Add(stepGo);
                qi.questSteps.Add(stepGo.AddComponent<SimpleQuestStep>());
            }

            return qi;
        }

        // ── InitializeQuest ───────────────────────────────────────────────────────

        [Test]
        public void InitializeQuest_NoRequirements_StateIsCanStart()
        {
            var qi = CreateQuestInfo();

            qi.InitializeQuest();

            Assert.AreEqual(QuestState.CAN_START, qi.state);
        }

        [Test]
        public void InitializeQuest_UnmetRequirements_StateIsRequirementsNotMet()
        {
            var prereqGo = new GameObject("Prereq");
            _created.Add(prereqGo);
            var prereq = prereqGo.AddComponent<QuestInfo>();
            prereq.questSteps = new List<QuestStep>();
            prereq.requirements = new List<QuestRequirement>();
            prereq.SetState(QuestState.IN_PROGRESS); // deliberately not FINISHED

            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { prereq } };
            var qi = CreateQuestInfo(requirements: new List<QuestRequirement> { req });

            qi.InitializeQuest();

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, qi.state);
        }

        [Test]
        public void InitializeQuest_MetRequirements_StateIsCanStart()
        {
            var prereqGo = new GameObject("Prereq");
            _created.Add(prereqGo);
            var prereq = prereqGo.AddComponent<QuestInfo>();
            prereq.questSteps = new List<QuestStep>();
            prereq.requirements = new List<QuestRequirement>();
            prereq.SetState(QuestState.FINISHED);

            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { prereq } };
            var qi = CreateQuestInfo(requirements: new List<QuestRequirement> { req });

            qi.InitializeQuest();

            Assert.AreEqual(QuestState.CAN_START, qi.state);
        }

        [Test]
        public void InitializeQuest_ResetsCurrentStepIndexToZero()
        {
            var qi = CreateQuestInfo(stepCount: 2);
            qi.InitializeQuest();
            qi.MoveToNextStep(); // move to step 1

            qi.InitializeQuest(); // re-initialize (e.g., repeatable quest)

            Assert.AreEqual(0, qi.currentStepIndex);
        }

        // ── CurrentStepExists ─────────────────────────────────────────────────────

        [Test]
        public void CurrentStepExists_WithSteps_ReturnsTrue()
        {
            var qi = CreateQuestInfo(stepCount: 2);
            qi.InitializeQuest();

            Assert.IsTrue(qi.CurrentStepExists());
        }

        [Test]
        public void CurrentStepExists_NoSteps_ReturnsFalse()
        {
            var qi = CreateQuestInfo(stepCount: 0);
            qi.InitializeQuest();

            Assert.IsFalse(qi.CurrentStepExists());
        }

        // ── MoveToNextStep ────────────────────────────────────────────────────────

        [Test]
        public void MoveToNextStep_IncrementsCurrentStepIndex()
        {
            var qi = CreateQuestInfo(stepCount: 2);
            qi.InitializeQuest();

            qi.MoveToNextStep();

            Assert.AreEqual(1, qi.currentStepIndex);
        }

        [Test]
        public void MoveToNextStep_PastAllSteps_CurrentStepExistsReturnsFalse()
        {
            var qi = CreateQuestInfo(stepCount: 1);
            qi.InitializeQuest();

            qi.MoveToNextStep();

            Assert.IsFalse(qi.CurrentStepExists());
        }

        // ── StoreQuestStepState / GetStepState / GetStepStatus ───────────────────

        [Test]
        public void StoreAndGetStepState_RoundTrip_ReturnsStoredStateString()
        {
            var qi = CreateQuestInfo(stepCount: 1);
            qi.InitializeQuest();

            qi.StoreQuestStepState(new QuestStepState("collected", QuestStepStatus.COMPLETED), 0);

            Assert.AreEqual("collected", qi.GetStepState(0));
        }

        [Test]
        public void GetStepStatus_AfterStore_ReturnsStoredStatus()
        {
            var qi = CreateQuestInfo(stepCount: 1);
            qi.InitializeQuest();

            qi.StoreQuestStepState(new QuestStepState("done", QuestStepStatus.COMPLETED), 0);

            Assert.AreEqual(QuestStepStatus.COMPLETED, qi.GetStepStatus(0));
        }

        [Test]
        public void GetStepState_DefaultAfterInit_IsEmpty()
        {
            var qi = CreateQuestInfo(stepCount: 1);
            qi.InitializeQuest();

            Assert.AreEqual("", qi.GetStepState(0));
        }

        [Test]
        public void GetStepStatus_DefaultAfterInit_IsNotStarted()
        {
            var qi = CreateQuestInfo(stepCount: 1);
            qi.InitializeQuest();

            Assert.AreEqual(QuestStepStatus.NOT_STARTED, qi.GetStepStatus(0));
        }

        [Test]
        public void GetStepState_BeforeInitialize_LogsErrorAndReturnsEmpty()
        {
            var qi = CreateQuestInfo(stepCount: 1);

            LogAssert.Expect(LogType.Error, new Regex("before InitializeQuest"));
            string result = qi.GetStepState(0);

            Assert.AreEqual("", result);
        }

        [Test]
        public void StoreQuestStepState_OutOfRangeIndex_LogsWarningAndDoesNotThrow()
        {
            var qi = CreateQuestInfo(stepCount: 1);
            qi.InitializeQuest();

            LogAssert.Expect(LogType.Warning, new Regex("out of range"));
            Assert.DoesNotThrow(() =>
                qi.StoreQuestStepState(new QuestStepState("x", QuestStepStatus.COMPLETED), 99));
        }

        // ── SetState / CheckRequirements ──────────────────────────────────────────

        [Test]
        public void SetState_UpdatesStateProperty()
        {
            var qi = CreateQuestInfo();

            qi.SetState(QuestState.IN_PROGRESS);

            Assert.AreEqual(QuestState.IN_PROGRESS, qi.state);
        }

        [Test]
        public void CheckRequirements_NullRequirements_ReturnsTrue()
        {
            var qi = CreateQuestInfo();
            qi.requirements = null;

            Assert.IsTrue(qi.CheckRequirements());
        }

        [Test]
        public void CheckRequirements_EmptyRequirements_ReturnsTrue()
        {
            var qi = CreateQuestInfo();

            Assert.IsTrue(qi.CheckRequirements());
        }

        [Test]
        public void ApplyAuthoringMetadata_SetsIdentityAndTurnInFlag()
        {
            var qi = CreateQuestInfo();

            qi.ApplyAuthoringMetadata("q05", "Mystery Egg", "Find the egg.", 0, true, null);

            Assert.AreEqual("q05", qi.id);
            Assert.AreEqual("Mystery Egg", qi.displayName);
            Assert.AreEqual("Find the egg.", qi.description);
            Assert.IsTrue(qi.waitForNpcTurnIn);
        }

        [Test]
        public void ApplyAuthoringMetadata_WithLevelAndPrerequisite_BuildsRequirement()
        {
            var qi = CreateQuestInfo();

            qi.ApplyAuthoringMetadata("q02", "Second Quest", "Desc", 3, false, "q01");

            Assert.AreEqual(1, qi.requirements.Count);
            Assert.AreEqual(3, qi.requirements[0].minPlayerLevel);
            CollectionAssert.AreEqual(new[] { "q01" }, qi.requirements[0].requiredQuestIds);
        }

        [Test]
        public void InitializeQuest_AfterApplyAuthoringMetadata_BlocksUntilPrerequisiteFinished()
        {
            var locatorGo = new GameObject("ServiceLocator");
            _created.Add(locatorGo);
            ServiceLocator locator = QuestSystemTestSupport.CreateServiceLocator(locatorGo);
            locator.Register<IPlayerLevelProvider>(new StubPlayerLevelProvider { Level = 10 });

            var questService = new StubQuestService();
            locator.Register<IQuestService>(questService);

            QuestInfo prereq = CreateQuestInfo();
            prereq.id = "q01";
            prereq.SetState(QuestState.IN_PROGRESS);
            questService.RegisterQuest(prereq);

            var qi = CreateQuestInfo();
            qi.ApplyAuthoringMetadata("q02", "Second", "Desc", 2, false, "q01");
            qi.InitializeQuest();

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, qi.state);

            prereq.SetState(QuestState.FINISHED);
            qi.InitializeQuest();

            Assert.AreEqual(QuestState.CAN_START, qi.state);

            QuestSystemTestSupport.ClearGlobalLocator();
        }
    }
}
