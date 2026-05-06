using RimMind.Contracts.Extension;
using RimMind.Dialogue.Settings;

namespace RimMind.Dialogue
{
    internal sealed class DialogueOverlayToggleBehavior : IToggleBehavior
    {
        public string Id => "dialogue_overlay";
        public bool IsActive => RimMindDialogueSettings.Get().overlayEnabled;
        public void Toggle()
        {
            var s = RimMindDialogueSettings.Get();
            s.overlayEnabled = !s.overlayEnabled;
            s.Write();
        }
    }
}
