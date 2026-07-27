using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace EnglishQuest.Tests.QuestSystem
{
    /// <summary>
    /// Integration tests for <see cref="QuestManager"/> covering the full
    /// <see cref="IQuestService"/> contract: initialization, quest start, step
    /// completion, quest finish, repeatable quests, and requirement unlocking.
    ///
    /// In Edit Mode, Awake does NOT fire when AddComponent is called.
    /// We inject <c>allQuestInfos</c> and the static singleton instance via
    /// reflection, then call <c>InitializeQuests</c> explicitly before each test.
    ///
    /// A bare <see cref="ServiceLocator"/> is injected into the private
    /// <c>global</c> static field so that <c>ServiceLocator.For(mb)</c> never
    /// falls through to <c>DontDestroyOnLoad</c> (forbidden in Edit Mode).
    /// </summary>
    [TestFixture]
    public class QuestManagerTests
    {
        // ── Inner test double ─────────────────────────────────────────────────────

        /// <summary>
        /// Minimal concrete <see cref="QuestStep"/> that exposes <c>FinishStep</c>
        /// to allow tests to drive step completion without needing triggers.
        /// </summary>
        private class TestQuestStep : QuestStep
        {
            public void TriggerFinish(string finalState = "") => FinishStep(finalState);
        }

        // ── Fixture state ─────────────────────────────────────────────────────────

        private GameObject _managerGo;
        private QuestManager _manager;
        private GameObject _serviceLocatorGo;
        private List<GameObject> _created;
        private LogLevel _previousLogLevel;

        [SetUp]
        public void SetUp()
        {
            _previousLogLevel = AppLog.Level;
            AppLog.Level = LogLevel.Warning;

            _created = new List<GameObject>();

            // ── Global ServiceLocator (Edit Mode safe) ────────────────────────────
            // ServiceLocator.For(mb) ultimately falls back to ServiceLocator.Global,
            // which auto-creates a GameObject and calls DontDestroyOnLoad — forbidden
            // in Edit Mode. We bypass this by creating a plain ServiceLocator and
            // injecting it into the private 'global' static field directly.
            _serviceLocatorGo = new GameObject("ServiceLocator [Global] (Test)");
            _created.Add(_serviceLocatorGo);
            var locator = _serviceLocatorGo.AddComponent<UnityServiceLocator.ServiceLocator>();
            typeof(UnityServiceLocator.ServiceLocator)
                .GetField("global", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, locator);

            // ── QuestManager ──────────────────────────────────────────────────────
            _managerGo = new GameObject("QuestManager_Test");
            _created.Add(_managerGo);
            _manager = _managerGo.AddComponent<QuestManager>();

            // Awake doesn't fire in Edit Mode — inject the singleton reference and
            // the IQuestService registration manually.
            SetStaticInstance(_manager);
            locator.Register<IQuestService>(_manager);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _created = null;
            _manager = null;

            // Guard against stale static references leaking to other fixtures.
            SetStaticInstance(null);
            typeof(UnityServiceLocator.ServiceLocator)
                .GetField("global", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

            AppLog.Level = _previousLogLevel;
        }

        // ── Reflection helpers ────────────────────────────────────────────────────

        private static void SetStaticInstance(QuestManager value)
        {
            typeof(StaticInstance<QuestManager>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, value);
        }

        private void SetAllQuestInfos(List<QuestInfo> quests)
        {
            typeof(QuestManager)
                .GetField("allQuestInfos", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_manager, quests);
        }

        private void CallInitializeQuests()
        {
            typeof(QuestManager)
                .GetMethod("InitializeQuests", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_manager, null);
        }

        // ── Quest / step factory helpers ──────────────────────────────────────────

        private QuestInfo CreateQuest(int stepCount = 0, bool isRepeatable = false,
            List<QuestRequirement> requirements = null)
        {
            var go = new GameObject("Quest");
            _created.Add(go);
            var qi = go.AddComponent<QuestInfo>();
            qi.questSteps = new List<QuestStep>();
            qi.requirements = requirements ?? new List<QuestRequirement>();
            qi.isRepeatable = isRepeatable;

            for (int i = 0; i < stepCount; i++)
            {
                var stepGo = new GameObject($"Step_{i}");
                _created.Add(stepGo);
                qi.questSteps.Add(stepGo.AddComponent<TestQuestStep>());
            }

            return qi;
        }

        // ── StartQuest ────────────────────────────────────────────────────────────

        [Test]
        public void StartQuest_CanStartState_ChangesStateToInProgress()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests(); // sets quest to CAN_START

            _manager.StartQuest(quest);

            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
        }

        [Test]
        public void StartQuest_CanStartState_FiresOnQuestStarted()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            QuestInfo received = null;
            _manager.OnQuestStarted += q => received = q;

            _manager.StartQuest(quest);

            Assert.AreSame(quest, received);
        }

        [Test]
        public void StartQuest_NonCanStartState_DoesNotChangeState()
        {
            var quest = CreateQuest();
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            quest.SetState(QuestState.IN_PROGRESS);

            LogAssert.Expect(LogType.Warning, new Regex("Cannot start quest"));
            _manager.StartQuest(quest);

            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
        }

        [Test]
        public void StartQuest_NonCanStartState_DoesNotFireOnQuestStarted()
        {
            var quest = CreateQuest();
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            quest.SetState(QuestState.IN_PROGRESS);

            bool fired = false;
            _manager.OnQuestStarted += _ => fired = true;

            LogAssert.Expect(LogType.Warning, new Regex("Cannot start quest"));
            _manager.StartQuest(quest);

            Assert.IsFalse(fired);
        }

        // ── IsQuestCompleted ──────────────────────────────────────────────────────

        [Test]
        public void IsQuestCompleted_FinishedQuest_ReturnsTrue()
        {
            var quest = CreateQuest();
            quest.SetState(QuestState.FINISHED);

            Assert.IsTrue(_manager.IsQuestCompleted(quest));
        }

        [Test]
        public void IsQuestCompleted_InProgressQuest_ReturnsFalse()
        {
            var quest = CreateQuest();
            quest.SetState(QuestState.IN_PROGRESS);

            Assert.IsFalse(_manager.IsQuestCompleted(quest));
        }

        [Test]
        public void IsQuestCompleted_NullArgument_ReturnsFalse()
        {
            Assert.IsFalse(_manager.IsQuestCompleted(null));
        }

        // ── RegisterQuestStep ─────────────────────────────────────────────────────

        [Test]
        public void RegisterQuestStep_NewStep_DoesNotThrow()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            var step = quest.questSteps[0];

            Assert.DoesNotThrow(() => _manager.RegisterQuestStep(step, quest, 0));
        }

        [Test]
        public void RegisterQuestStep_DuplicateStep_IgnoresSecondRegistration()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            var step = quest.questSteps[0];
            _manager.RegisterQuestStep(step, quest, 0);

            Assert.DoesNotThrow(() => _manager.RegisterQuestStep(step, quest, 0));
        }

        // ── Step completion → quest advancement ───────────────────────────────────

        [Test]
        public void StepCompletion_SingleStep_QuestBecomesFinished()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step = quest.questSteps[0] as TestQuestStep;
            _manager.RegisterQuestStep(step, quest, 0);

            step.TriggerFinish("done");

            Assert.AreEqual(QuestState.FINISHED, quest.state);
        }

        [Test]
        public void StepCompletion_SingleStep_FiresOnQuestCompleted()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step = quest.questSteps[0] as TestQuestStep;
            _manager.RegisterQuestStep(step, quest, 0);

            QuestInfo completed = null;
            _manager.OnQuestCompleted += q => completed = q;

            step.TriggerFinish();

            Assert.AreSame(quest, completed);
        }

        [Test]
        public void StepCompletion_StoresStepStateAsCompleted()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step = quest.questSteps[0] as TestQuestStep;
            _manager.RegisterQuestStep(step, quest, 0);

            step.TriggerFinish("collected");

            Assert.AreEqual("collected", quest.GetStepState(0));
            Assert.AreEqual(QuestStepStatus.COMPLETED, quest.GetStepStatus(0));
        }

        [Test]
        public void StepCompletion_TwoSteps_FirstStepAdvancesIndexToOne()
        {
            var quest = CreateQuest(stepCount: 2);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step0 = quest.questSteps[0] as TestQuestStep;
            _manager.RegisterQuestStep(step0, quest, 0);

            step0.TriggerFinish();

            Assert.AreEqual(1, quest.currentStepIndex);
        }

        [Test]
        public void StepCompletion_TwoSteps_QuestRemainsInProgress()
        {
            var quest = CreateQuest(stepCount: 2);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step0 = quest.questSteps[0] as TestQuestStep;
            _manager.RegisterQuestStep(step0, quest, 0);

            step0.TriggerFinish();

            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
        }

        [Test]
        public void StepCompletion_TwoSteps_FirstStepFiresOnQuestUpdated()
        {
            var quest = CreateQuest(stepCount: 2);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step0 = quest.questSteps[0] as TestQuestStep;
            _manager.RegisterQuestStep(step0, quest, 0);

            QuestInfo updated = null;
            _manager.OnQuestUpdated += q => updated = q;

            step0.TriggerFinish();

            Assert.AreSame(quest, updated);
        }

        // ── FinishQuest / repeatable quests ───────────────────────────────────────

        [Test]
        public void FinishQuest_CanFinishState_QuestBecomesFinished()
        {
            var quest = CreateQuest();
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            quest.SetState(QuestState.CAN_FINISH);

            _manager.FinishQuest(quest);

            Assert.AreEqual(QuestState.FINISHED, quest.state);
        }

        [Test]
        public void FinishQuest_RepeatableQuest_ResetsToCanStart()
        {
            var quest = CreateQuest(isRepeatable: true);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            quest.SetState(QuestState.CAN_FINISH);

            _manager.FinishQuest(quest);

            // InitializeQuest is called again on repeatable quests → CAN_START
            Assert.AreEqual(QuestState.CAN_START, quest.state);
        }

        [Test]
        public void FinishQuest_NonCanFinishState_DoesNotChangeState()
        {
            var quest = CreateQuest();
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            // State is CAN_START after init — not CAN_FINISH

            _manager.FinishQuest(quest);

            Assert.AreEqual(QuestState.CAN_START, quest.state);
        }

        // ── GetQuestByInfo ────────────────────────────────────────────────────────

        [Test]
        public void GetQuestByInfo_ManagedQuest_ReturnsIt()
        {
            var quest = CreateQuest();
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            var result = _manager.GetQuestByInfo(quest);

            Assert.AreSame(quest, result);
        }

        [Test]
        public void GetQuestByInfo_UnmanagedQuest_ReturnsNull()
        {
            SetAllQuestInfos(new List<QuestInfo>());
            CallInitializeQuests();

            var externalGo = new GameObject("External");
            _created.Add(externalGo);
            var external = externalGo.AddComponent<QuestInfo>();
            external.questSteps = new List<QuestStep>();
            external.requirements = new List<QuestRequirement>();

            var result = _manager.GetQuestByInfo(external);

            Assert.IsNull(result);
        }

        // ── Requirement unlocking ─────────────────────────────────────────────────

        [Test]
        public void FinishingPrerequisiteQuest_UnlocksRequirementNotMetQuest()
        {
            var prereq = CreateQuest(stepCount: 0);
            var dependent = CreateQuest(stepCount: 0);

            var req = new QuestRequirement { requiredQuests = new List<QuestInfo> { prereq } };
            dependent.requirements = new List<QuestRequirement> { req };

            SetAllQuestInfos(new List<QuestInfo> { prereq, dependent });
            CallInitializeQuests();

            // After init: prereq → CAN_START; dependent → REQUIREMENTS_NOT_MET
            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, dependent.state);

            prereq.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(prereq);

            // CheckRequirementsForOtherQuests fires inside FinishQuest → dependent unlocked
            Assert.AreEqual(QuestState.CAN_START, dependent.state);
        }

        [Test]
        public void FinishingQuest_DoesNotUnlockQuestWithStillUnmetRequirements()
        {
            var prereq1 = CreateQuest();
            var prereq2 = CreateQuest();
            var dependent = CreateQuest();

            prereq1.SetState(QuestState.IN_PROGRESS);
            prereq2.SetState(QuestState.IN_PROGRESS);

            var req = new QuestRequirement
                { requiredQuests = new List<QuestInfo> { prereq1, prereq2 } };
            dependent.requirements = new List<QuestRequirement> { req };

            SetAllQuestInfos(new List<QuestInfo> { prereq1, prereq2, dependent });
            CallInitializeQuests();

            // Finish only prereq1 — prereq2 is still not done
            prereq1.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(prereq1);

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, dependent.state);
        }

        [Test]
        public void RegisterQuest_AddsQuestAndInitializesIt()
        {
            SetAllQuestInfos(new List<QuestInfo>());
            CallInitializeQuests();

            var quest = CreateQuest();
            quest.id = "registered_quest";
            _manager.RegisterQuest(quest);

            Assert.AreEqual(1, _manager.AllQuests.Count);
            Assert.AreEqual(QuestState.CAN_START, quest.state);
            Assert.AreSame(quest, _manager.GetQuestById("registered_quest"));
        }

        [Test]
        public void CompletingLastStep_WithWaitForNpcTurnIn_StaysAtCanFinish()
        {
            var quest = CreateQuest(stepCount: 1);
            quest.waitForNpcTurnIn = true;
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step = quest.questSteps[0] as TestQuestStep;
            step.TriggerFinish();

            Assert.AreEqual(QuestState.CAN_FINISH, quest.state);
        }

        [Test]
        public void CompletingLastStep_WithoutWaitForNpcTurnIn_FinishesImmediately()
        {
            var quest = CreateQuest(stepCount: 1);
            quest.waitForNpcTurnIn = false;
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            _manager.StartQuest(quest);

            var step = quest.questSteps[0] as TestQuestStep;
            step.TriggerFinish();

            Assert.AreEqual(QuestState.FINISHED, quest.state);
        }

        [Test]
        public void LevelRequirement_BlocksQuestUntilLevelIsMet()
        {
            var locator = _serviceLocatorGo.GetComponent<ServiceLocator>();
            locator.Register<IPlayerLevelProvider>(new StubPlayerLevelProvider { Level = 1 });

            var quest = CreateQuest(requirements: new List<QuestRequirement>
            {
                new QuestRequirement { minPlayerLevel = 5 }
            });

            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, quest.state);
        }

        [Test]
        public void LevelUp_UnlocksQuestWhenLevelRequirementMet()
        {
            var locator = _serviceLocatorGo.GetComponent<ServiceLocator>();
            var levelProvider = new StubPlayerLevelProvider { Level = 1 };
            locator.Register<IPlayerLevelProvider>(levelProvider);

            var quest = CreateQuest(requirements: new List<QuestRequirement>
            {
                new QuestRequirement { minPlayerLevel = 3 }
            });

            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, quest.state);

            levelProvider.Level = 3;
            _manager.ReevaluateQuestRequirements();

            Assert.AreEqual(QuestState.CAN_START, quest.state);
        }

        [Test]
        public void FinishingPrerequisite_DoesNotUnlockIfLevelTooLow()
        {
            var locator = _serviceLocatorGo.GetComponent<ServiceLocator>();
            locator.Register<IPlayerLevelProvider>(new StubPlayerLevelProvider { Level = 1 });

            var prereq = CreateQuest(stepCount: 0);
            prereq.id = "q01";
            var dependent = CreateQuest(stepCount: 0);
            dependent.id = "q02";
            dependent.requirements = new List<QuestRequirement>
            {
                new QuestRequirement
                {
                    minPlayerLevel = 5,
                    requiredQuestIds = new List<string> { "q01" }
                }
            };

            SetAllQuestInfos(new List<QuestInfo> { prereq, dependent });
            CallInitializeQuests();

            prereq.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(prereq);

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, dependent.state);
        }

        [Test]
        public void ApplyProgressEntry_CanStartDowngradedWhenLevelTooLow()
        {
            var locator = _serviceLocatorGo.GetComponent<ServiceLocator>();
            locator.Register<IPlayerLevelProvider>(new StubPlayerLevelProvider { Level = 1 });

            var quest = CreateQuest(requirements: new List<QuestRequirement>
            {
                new QuestRequirement { minPlayerLevel = 5 }
            });
            quest.id = "q_level_gate";

            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            quest.ApplyProgressEntry(new QuestProgressEntry
            {
                State = (int)QuestState.CAN_START,
                CurrentStepIndex = 0,
                Steps = new List<StepProgressEntry>()
            });

            Assert.AreEqual(QuestState.CAN_START, quest.state);

            _manager.ReevaluateQuestRequirements();

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, quest.state);
        }

        [Test]
        public void StartQuest_RejectsWhenRequirementsNoLongerMet()
        {
            var locator = _serviceLocatorGo.GetComponent<ServiceLocator>();
            locator.Register<IPlayerLevelProvider>(new StubPlayerLevelProvider { Level = 1 });

            var quest = CreateQuest(requirements: new List<QuestRequirement>
            {
                new QuestRequirement { minPlayerLevel = 5 }
            });

            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();
            quest.SetState(QuestState.CAN_START);

            LogAssert.Expect(LogType.Warning, new Regex("Requirements are no longer met"));
            _manager.StartQuest(quest);

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, quest.state);
        }

        [Test]
        public void FinishingPrerequisiteById_UnlocksDependentQuest()
        {
            var prereq = CreateQuest(stepCount: 0);
            prereq.id = "q01";
            var dependent = CreateQuest(stepCount: 0);
            dependent.id = "q02";
            dependent.requirements = new List<QuestRequirement>
            {
                new QuestRequirement { requiredQuestIds = new List<string> { "q01" } }
            };

            SetAllQuestInfos(new List<QuestInfo> { prereq, dependent });
            CallInitializeQuests();

            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, dependent.state);

            prereq.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(prereq);

            Assert.AreEqual(QuestState.CAN_START, dependent.state);
        }

        [Test]
        public void StartQuest_FiresOnQuestStateChanged()
        {
            var quest = CreateQuest(stepCount: 1);
            SetAllQuestInfos(new List<QuestInfo> { quest });
            CallInitializeQuests();

            QuestInfo changed = null;
            _manager.OnQuestStateChanged += q => changed = q;

            _manager.StartQuest(quest);

            Assert.AreSame(quest, changed);
            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
        }

        [Test]
        public void FinishQuest_LastMandatoryQuest_FiresLevelCompleted()
        {
            var questA = CreateQuest();
            var questB = CreateQuest();
            SetAllQuestInfos(new List<QuestInfo> { questA, questB });
            CallInitializeQuests();

            bool levelCompletedFired = false;
            _manager.OnLevelCompleted += () => levelCompletedFired = true;

            questA.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(questA);

            Assert.IsFalse(_manager.IsLevelCompleted);

            questB.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(questB);

            Assert.IsTrue(_manager.IsLevelCompleted);
            Assert.IsTrue(levelCompletedFired);
        }

        private sealed class StubPlayerLevelProvider : IPlayerLevelProvider
        {
            public int Level = 1;
            public int CurrentLevel => Level;
        }
    }
}

