using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class StrengthModifierMetabolismComponent : Component
{
    [ViewVariables]
    public int StrengthModifier { get; set; }

    /// <summary>
    /// When the current modifier is expected to end.
    /// </summary>
    [ViewVariables]
    public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;

    [Serializable, NetSerializable]
    public sealed class StrengthModifierMetabolismComponentState : ComponentState
    {
        public int StrengthModifier { get; }
        public TimeSpan ModifierTimer { get; }

        public StrengthModifierMetabolismComponentState(int strengthModifier, TimeSpan modifierTimer)
        {
            StrengthModifier = strengthModifier;
            ModifierTimer = modifierTimer;
        }
    }
}


