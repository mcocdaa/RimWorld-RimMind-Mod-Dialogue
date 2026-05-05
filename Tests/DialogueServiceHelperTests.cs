using RimMind.Dialogue.Core;
using Xunit;

namespace RimMind.Dialogue.Tests
{
    public class DialogueServiceHelperTests
    {
        [Fact]
        public void MakePairKey_SmallerFirst_ReturnsOrdered()
        {
            var key = DialogueClassifier.MakePairKey(1, 5);
            Assert.Equal((1, 5), key);
        }

        [Fact]
        public void MakePairKey_LargerFirst_ReturnsOrdered()
        {
            var key = DialogueClassifier.MakePairKey(5, 1);
            Assert.Equal((1, 5), key);
        }

        [Fact]
        public void MakePairKey_SameIds_ReturnsSame()
        {
            var key = DialogueClassifier.MakePairKey(3, 3);
            Assert.Equal((3, 3), key);
        }

        [Fact]
        public void MakePairKey_NegativeIds_ReturnsOrdered()
        {
            var key = DialogueClassifier.MakePairKey(-1, 5);
            Assert.Equal((-1, 5), key);
        }

        [Fact]
        public void MakePairKey_IsCommutative()
        {
            var key1 = DialogueClassifier.MakePairKey(10, 20);
            var key2 = DialogueClassifier.MakePairKey(20, 10);
            Assert.Equal(key1, key2);
        }

        [Theory]
        [InlineData(DialogueTriggerType.Auto, true, true, DialogueCategory.ColonistDialogue)]
        [InlineData(DialogueTriggerType.Hediff, true, false, DialogueCategory.NonColonistDialogue)]
        [InlineData(DialogueTriggerType.Thought, false, true, DialogueCategory.NonColonistDialogue)]
        public void Classify_WithRecipient(DialogueTriggerType trigger, bool initiatorColonist, bool recipientColonist, DialogueCategory expected)
        {
            DialogueCategory result = DialogueClassifier.Classify(initiatorColonist, recipientColonist, trigger);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Classify_NullRecipient_ColonistMonologue()
        {
            Assert.Equal(DialogueCategory.ColonistMonologue, DialogueClassifier.Classify(true, null, DialogueTriggerType.Chitchat));
        }

        [Fact]
        public void Classify_NullRecipient_NonColonistMonologue()
        {
            Assert.Equal(DialogueCategory.NonColonistMonologue, DialogueClassifier.Classify(false, null, DialogueTriggerType.Chitchat));
        }

        [Fact]
        public void Classify_PlayerInput_AlwaysPlayerDialogue()
        {
            Assert.Equal(DialogueCategory.PlayerDialogue, DialogueClassifier.Classify(false, false, DialogueTriggerType.PlayerInput));
            Assert.Equal(DialogueCategory.PlayerDialogue, DialogueClassifier.Classify(true, true, DialogueTriggerType.PlayerInput));
        }

        [Fact]
        public void Classify_PlayerInput_NullRecipient_StillPlayerDialogue()
        {
            Assert.Equal(DialogueCategory.PlayerDialogue, DialogueClassifier.Classify(true, null, DialogueTriggerType.PlayerInput));
        }

        [Fact]
        public void Classify_BothColonist_ColonistDialogue()
        {
            Assert.Equal(DialogueCategory.ColonistDialogue, DialogueClassifier.Classify(true, true, DialogueTriggerType.Chitchat));
        }

        [Fact]
        public void Classify_InitiatorNonColonist_NonColonistDialogue()
        {
            Assert.Equal(DialogueCategory.NonColonistDialogue, DialogueClassifier.Classify(false, true, DialogueTriggerType.Auto));
        }
    }
}
