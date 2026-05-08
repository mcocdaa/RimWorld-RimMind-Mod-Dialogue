using RimMind.Contracts.Extension;
using RimMind.Dialogue.Core;
using RimWorld;
using Verse;

namespace RimMind.Dialogue
{
    internal sealed class DialogueTriggerAdapter : IDialogueTrigger
    {
        public string Id => "dialogue";
        public void Trigger(object pawn, string context, object? recipient) =>
            RimMindDialogueService.HandleTrigger((Pawn)pawn, context, DialogueTriggerType.Chitchat, recipient as Pawn);
    }
}
