using NUnit.Framework;

namespace EnglishQuest.Tests.QuestSystem
{
    /// <summary>
    /// Pure unit tests for <see cref="QuestStepState"/>.
    /// No Unity runtime needed — verifies the data model's constructors and field syncing.
    /// </summary>
    [TestFixture]
    public class QuestStepStateTests
    {
        [Test]
        public void DefaultConstructor_State_IsEmpty()
        {
            var state = new QuestStepState();

            Assert.AreEqual("", state.state);
        }

        [Test]
        public void DefaultConstructor_StepStatus_IsNotStarted()
        {
            var state = new QuestStepState();

            Assert.AreEqual(QuestStepStatus.NOT_STARTED, state.stepStatus);
        }

        [Test]
        public void DefaultConstructor_StatusString_IsEmpty()
        {
            var state = new QuestStepState();

            Assert.AreEqual("", state.status);
        }

        [Test]
        public void ParameterizedConstructor_SetsStateString()
        {
            var state = new QuestStepState("half-done", QuestStepStatus.IN_PROGRESS);

            Assert.AreEqual("half-done", state.state);
        }

        [Test]
        public void ParameterizedConstructor_SetsStepStatusEnum()
        {
            var state = new QuestStepState("half-done", QuestStepStatus.IN_PROGRESS);

            Assert.AreEqual(QuestStepStatus.IN_PROGRESS, state.stepStatus);
        }

        [Test]
        public void ParameterizedConstructor_SyncsStatusStringFromEnum()
        {
            var state = new QuestStepState("done", QuestStepStatus.COMPLETED);

            Assert.AreEqual(QuestStepStatus.COMPLETED.ToString(), state.status);
        }

        [Test]
        public void ParameterizedConstructor_NotStarted_SyncsStatusString()
        {
            var state = new QuestStepState("", QuestStepStatus.NOT_STARTED);

            Assert.AreEqual(QuestStepStatus.NOT_STARTED.ToString(), state.status);
        }

        [Test]
        public void ParameterizedConstructor_EmptyStateString_IsAllowed()
        {
            var state = new QuestStepState("", QuestStepStatus.COMPLETED);

            Assert.AreEqual("", state.state);
            Assert.AreEqual(QuestStepStatus.COMPLETED, state.stepStatus);
        }
    }
}

