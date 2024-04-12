using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Nuclear14.Special;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a PerceptionModifier on the target,
    /// adding one if not there and to change the perception
    /// </summary>
public sealed partial class PerceptionModifierReagent : ReagentEffect
{

    [DataField("perceptionModifier")]
    public int PerceptionModifier { get; set; } = 0;

    /// <summary>
    /// How long the modifier applies (in seconds) when metabolized.
    /// </summary>
    [DataField("statusLifetime")]
    public float StatusLifetime = 2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-perception-modifier",
            ("chance", Probability),
            ("perception", PerceptionModifier),
            ("time", StatusLifetime));
    }

    /// <summary>
    /// Remove reagent at set rate, changes the perception modifier and adds a PerceptionModifierMetabolismComponent if not already there.
    /// </summary>
    public override void Effect(ReagentEffectArgs args)
    {
        var status = args.EntityManager.EnsureComponent<PerceptionModifierMetabolismComponent>(args.SolutionEntity);

        // Only refresh movement if we need to.
        var modified = !status.PerceptionModifier.Equals(PerceptionModifier);

        status.PerceptionModifier = PerceptionModifier;

        // only going to scale application time
        var statusLifetime = StatusLifetime;
        statusLifetime *= args.Scale;

        IncreaseTimer(status, statusLifetime);

        if (modified)
            EntitySystem.Get<SpecialModifierSystem>().RefreshClothingSpecialModifiers(args.SolutionEntity);

    }
    public void IncreaseTimer(PerceptionModifierMetabolismComponent status, float time)
    {
        var gameTiming = IoCManager.Resolve<IGameTiming>();

        var offsetTime = Math.Max(status.ModifierTimer.TotalSeconds, gameTiming.CurTime.TotalSeconds);

        status.ModifierTimer = TimeSpan.FromSeconds(offsetTime + time);
        status.Dirty();
    }
}
