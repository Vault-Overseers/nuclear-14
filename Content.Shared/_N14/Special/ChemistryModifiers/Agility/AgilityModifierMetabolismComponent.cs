/*
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._N14.Special.ChemistryModifiers;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class AgilityModifierMetabolismComponent : Component
{
    [ViewVariables]
    public int AgilityModifier { get; set; }

    /// <summary>
    /// When the current modifier is expected to end.
    /// </summary>
    [ViewVariables]
    public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;

    [Serializable, NetSerializable]
    public sealed class AgilityModifierMetabolismComponentState : ComponentState
    {
        public int AgilityModifier { get; }
        public TimeSpan ModifierTimer { get; }

        public AgilityModifierMetabolismComponentState(int agilityModifier, TimeSpan modifierTimer)
        {
            AgilityModifier = agilityModifier;
            ModifierTimer = modifierTimer;
        }
    }
}


*/
