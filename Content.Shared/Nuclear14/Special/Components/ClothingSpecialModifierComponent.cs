using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;

namespace Content.Shared.Nuclear14.Special.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(ClothingSpecialModifierSystem))]
public sealed partial class ClothingSpecialModifierComponent : Component
{
    #region Special stats

    /// <summary>
    /// SPECIAL Strength of cloth boost
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("strengthModifier")]
    public int StrengthModifier = 0;

    /// <summary>
    /// SPECIAL Perception of cloth boost
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("perceptionModifier")]
    public int PerceptionModifier = 0;

    /// <summary>
    /// SPECIAL Endurance of cloth boost
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enduranceModifier")]
    public int EnduranceModifier = 0;

    /// <summary>
    /// SPECIAL Charisma of cloth boost
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("charismaModifier")]
    public int CharismaModifier = 0;

    /// <summary>
    /// SPECIAL Intelligence of cloth boost
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("intelligenceModifier")]
    public int IntelligenceModifier = 0;

    /// <summary>
    /// SPECIAL Agility of cloth boost
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("agilityModifier")]
    public int AgilityModifier = 0;

    /// <summary>
    /// SPECIAL Luck of cloth boost
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("luckModifier")]
    public int LuckModifier = 0;

    /// <summary>
    ///     Is this clothing item currently 'actively' affects you?
    ///     e.g. magboots can be turned on and off.
    ///     e.g. or clothing using something battery which could become dead
    /// </summary>
    [DataField("enabled")] public bool Enabled = true;


    #endregion

}

[Serializable, NetSerializable]
public sealed class ClothingSpecialModifierComponentState : ComponentState
{
    public int StrengthModifier;
    public int PerceptionModifier;
    public int EnduranceModifier;
    public int CharismaModifier;
    public int IntelligenceModifier;
    public int AgilityModifier;
    public int LuckModifier;
    public bool Enabled;

    public ClothingSpecialModifierComponentState(
        int strengthModifier,
        int perceptionModifier,
        int enduranceModifier,
        int charismaModifier,
        int intelligenceModifier,
        int agilityModifier,
        int luckModifier,
        bool enabled)
    {
        StrengthModifier = strengthModifier;
        PerceptionModifier = perceptionModifier;
        EnduranceModifier = enduranceModifier;
        CharismaModifier = charismaModifier;
        IntelligenceModifier = intelligenceModifier;
        AgilityModifier = agilityModifier;
        LuckModifier = luckModifier;
        Enabled = enabled;
    }
}
