using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class PerceptionModifierMetabolismComponent : Component
{
    [ViewVariables]
    public int PerceptionModifier { get; set; }

    /// <summary>
    /// When the current modifier is expected to end.
    /// </summary>
    [ViewVariables]
    public TimeSpan ModifierTimer { get; set; } = TimeSpan.Zero;

    [Serializable, NetSerializable]
    public sealed class PerceptionModifierMetabolismComponentState : ComponentState
    {
        public int PerceptionModifier { get; }
        public TimeSpan ModifierTimer { get; }

        public PerceptionModifierMetabolismComponentState(int perceptionModifier, TimeSpan modifierTimer)
        {
            PerceptionModifier = perceptionModifier;
            ModifierTimer = modifierTimer;
        }
    }
}


