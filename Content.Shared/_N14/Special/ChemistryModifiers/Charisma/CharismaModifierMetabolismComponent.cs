/*
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._N14.Special.ChemistryModifiers;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class CharismaModifierMetabolismComponent : Component
{
    [ViewVariables]
    public int CharismaModifier { get; set; }

    /// <summary>
    /// When the current modifier is expected to end.
    /// </summary>
    [ViewVariables]
    public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;

    [Serializable, NetSerializable]
    public sealed class CharismaModifierMetabolismComponentState : ComponentState
    {
        public int CharismaModifier { get; }
        public TimeSpan ModifierTimer { get; }

        public CharismaModifierMetabolismComponentState(int charismaModifier, TimeSpan modifierTimer)
        {
            CharismaModifier = charismaModifier;
            ModifierTimer = modifierTimer;
        }
    }
}


*/
