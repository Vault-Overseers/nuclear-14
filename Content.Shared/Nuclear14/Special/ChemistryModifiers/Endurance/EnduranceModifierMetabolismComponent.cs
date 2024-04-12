using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class EnduranceModifierMetabolismComponent : Component
{
    [ViewVariables]
    public int EnduranceModifier { get; set; }

    /// <summary>
    /// When the current modifier is expected to end.
    /// </summary>
    [ViewVariables]
    public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;

    [Serializable, NetSerializable]
    public sealed class EnduranceModifierMetabolismComponentState : ComponentState
    {
        public int EnduranceModifier { get; }
        public TimeSpan ModifierTimer { get; }

        public EnduranceModifierMetabolismComponentState(int enduranceModifier, TimeSpan modifierTimer)
        {
            EnduranceModifier = enduranceModifier;
            ModifierTimer = modifierTimer;
        }
    }
}


