using RimMind.Contracts.Extension;
using RimMind.Dialogue.Core;
using RimWorld;
using Verse;

namespace RimMind.Dialogue
{
    internal sealed class DialogueTriggerAdapter : IDialogueTrigger
    {
        public string Id => "dialogue";
        public void Trigger(Pawn pawn, string context, Pawn? recipient) =>
            RimMindDialogueService.HandleTrigger(pawn, context, DialogueTriggerType.Chitchat, recipient);
    }
}
