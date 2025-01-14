/*
using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._N14.Special.Components;

[RegisterComponent, NetworkedComponent]//, AutoGenerateComponentState]
public sealed partial class SpecialComponent : Component
{
    #region Special Base Stats

    /// <summary>
    /// SPECIAL Strength of player
    /// </summary>
    ///
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("strength")]
    public int BaseStrength { get; set; } = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("perception")]
    public int BasePerception { get; set; } = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("endurance")]
    public int BaseEndurance { get; set; } = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("charisma")]
    public int BaseCharisma { get; set; } = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("intelligence")]
    public int BaseIntelligence { get; set; } = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("agility")]
    public int BaseAgility { get; set; } = 5;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("luck")]
    public int BaseLuck { get; set; } = 5;

    #endregion

    #region Special modifiers

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("strengthModifier")]
    public int StrengthModifier { get; set; } = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("perceptionModifier")]
    public int PerceptionModifier { get; set; } = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enduranceModifier")]
    public int EnduranceModifier { get; set; } = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("charismaModifier")]
    public int CharismaModifier { get; set; } = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("intelligenceModifier")]
    public int IntelligenceModifier { get; set; } = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("agilityModifier")]
    public int AgilityModifier { get; set; } = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("luckModifier")]
    public int LuckModifier { get; set; } = 0;

    #endregion

    #region Total Special

    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalStrength { get { return Math.Clamp(BaseStrength + StrengthModifier, SpecialAmountMin, SpecialAmountMax); } }

    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalPerception { get { return Math.Clamp(BasePerception + PerceptionModifier, SpecialAmountMin, SpecialAmountMax); } }

    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalEndurance { get { return Math.Clamp(BaseEndurance + EnduranceModifier, SpecialAmountMin, SpecialAmountMax); } }

    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalCharisma { get { return Math.Clamp(BaseCharisma + CharismaModifier, SpecialAmountMin, SpecialAmountMax); } }

    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalIntelligence { get { return Math.Clamp(BaseIntelligence + IntelligenceModifier, SpecialAmountMin, SpecialAmountMax); } }

    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalAgility { get { return Math.Clamp(BaseAgility + AgilityModifier, SpecialAmountMin, SpecialAmountMax); } }

    [ViewVariables(VVAccess.ReadWrite)]
    public int TotalLuck { get { return Math.Clamp(BaseLuck + LuckModifier, SpecialAmountMin, SpecialAmountMax); } }

    #endregion

    #region Special min/max amount

    /// <summary>
    ///     Don't let Special go above this value.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]//, AutoNetworkedField]
    public int SpecialAmountMax = 10;

    /// <summary>
    ///     Don't let Special go below this value.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]//, AutoNetworkedField]
    public int SpecialAmountMin = 1;

    #endregion

}
*/
