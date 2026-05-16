using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Dialogue.Settings;

namespace RimMind.Dialogue
{
    internal sealed class DialogueModCooldown : IModCooldown
    {
        public string Id => "Dialogue";
        public int CooldownTicks => RimMindDialogueSettings.Get().monologueCooldownTicks;
    }
}
