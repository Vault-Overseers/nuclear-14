using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Nuclear14.Special;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a IntelligenceModifier on the target,
    /// adding one if not there and to change the Intelligence
    /// </summary>
public sealed partial class IntelligenceModifierReagent : ReagentEffect
{

    [DataField("intelligenceModifier")]
    public int IntelligenceModifier { get; set; } = 0;

    /// <summary>
    /// How long the modifier applies (in seconds) when metabolized.
    /// </summary>
    [DataField("statusLifetime")]
    public float StatusLifetime = 2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-intelligence-modifier",
            ("chance", Probability),
            ("intelligence", IntelligenceModifier),
            ("time", StatusLifetime));
    }

    /// <summary>
    /// Remove reagent at set rate, changes the Intelligence modifier and adds a IntelligenceModifierMetabolismComponent if not already there.
    /// </summary>
    public override void Effect(ReagentEffectArgs args)
    {
        var status = args.EntityManager.EnsureComponent<IntelligenceModifierMetabolismComponent>(args.SolutionEntity);

        // Only refresh movement if we need to.
        var modified = !status.IntelligenceModifier.Equals(IntelligenceModifier);

        status.IntelligenceModifier = IntelligenceModifier;

        // only going to scale application time
        var statusLifetime = StatusLifetime;
        statusLifetime *= args.Scale;

        IncreaseTimer(status, statusLifetime);

        if (modified)
            EntitySystem.Get<SpecialModifierSystem>().RefreshClothingSpecialModifiers(args.SolutionEntity);

    }
    public void IncreaseTimer(IntelligenceModifierMetabolismComponent status, float time)
    {
        var gameTiming = IoCManager.Resolve<IGameTiming>();

        var offsetTime = Math.Max(status.ModifierTimer.TotalSeconds, gameTiming.CurTime.TotalSeconds);

        status.ModifierTimer = TimeSpan.FromSeconds(offsetTime + time);
        status.Dirty();
    }
}
