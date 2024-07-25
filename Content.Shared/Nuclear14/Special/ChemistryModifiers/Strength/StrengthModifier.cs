using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Nuclear14.Special;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a StrengthModifier on the target,
    /// adding one if not there and to change the strength
    /// </summary>
public sealed partial class StrengthModifierReagent : ReagentEffect
{

    [DataField("strengthModifier")]
    public int StrengthModifier { get; set; } = 0;

    /// <summary>
    /// How long the modifier applies (in seconds) when metabolized.
    /// </summary>
    [DataField("statusLifetime")]
    public float StatusLifetime = 2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-strength-modifier",
            ("chance", Probability),
            ("strength", StrengthModifier),
            ("time", StatusLifetime));
    }

    /// <summary>
    /// Remove reagent at set rate, changes the strength modifier and adds a StrengthModifierMetabolismComponent if not already there.
    /// </summary>
    public override void Effect(ReagentEffectArgs args)
    {
        var status = args.EntityManager.EnsureComponent<StrengthModifierMetabolismComponent>(args.SolutionEntity);

        // Only refresh movement if we need to.
        var modified = !status.StrengthModifier.Equals(StrengthModifier);

        status.StrengthModifier = StrengthModifier;

        // only going to scale application time
        var statusLifetime = StatusLifetime;
        statusLifetime *= args.Scale;

        IncreaseTimer(status, statusLifetime);

        if (modified)
            EntitySystem.Get<SpecialModifierSystem>().RefreshClothingSpecialModifiers(args.SolutionEntity);

    }
    public void IncreaseTimer(StrengthModifierMetabolismComponent status, float time)
    {
        var gameTiming = IoCManager.Resolve<IGameTiming>();

        var offsetTime = Math.Max(status.ModifierTimer.TotalSeconds, gameTiming.CurTime.TotalSeconds);

        status.ModifierTimer = TimeSpan.FromSeconds(offsetTime + time);
        status.Dirty();
    }
}
