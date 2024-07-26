using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class LuckModifierMetabolismComponent : Component
{
    [ViewVariables]
    public int LuckModifier { get; set; }

    /// <summary>
    /// When the current modifier is expected to end.
    /// </summary>
    [ViewVariables]
    public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;

    [Serializable, NetSerializable]
    public sealed class LuckModifierMetabolismComponentState : ComponentState
    {
        public int LuckModifier { get; }
        public TimeSpan ModifierTimer { get; }

        public LuckModifierMetabolismComponentState(int luckModifier, TimeSpan modifierTimer)
        {
            LuckModifier = luckModifier;
            ModifierTimer = modifierTimer;
        }
    }
}


