using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Nuclear14.Special;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;

    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a CharismaModifier on the target,
    /// adding one if not there and to change the Charisma
    /// </summary>
public sealed partial class CharismaModifierReagent : ReagentEffect
{

    [DataField("charismaModifier")]
    public int CharismaModifier { get; set; } = 0;

    /// <summary>
    /// How long the modifier applies (in seconds) when metabolized.
    /// </summary>
    [DataField("statusLifetime")]
    public float StatusLifetime = 2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-charisma-modifier",
            ("chance", Probability),
            ("charisma", CharismaModifier),
            ("time", StatusLifetime));
    }

    /// <summary>
    /// Remove reagent at set rate, changes the Charisma modifier and adds a CharismaModifierMetabolismComponent if not already there.
    /// </summary>
    public override void Effect(ReagentEffectArgs args)
    {
        var status = args.EntityManager.EnsureComponent<CharismaModifierMetabolismComponent>(args.SolutionEntity);

        // Only refresh movement if we need to.
        var modified = !status.CharismaModifier.Equals(CharismaModifier);

        status.CharismaModifier = CharismaModifier;

        // only going to scale application time
        var statusLifetime = StatusLifetime;
        statusLifetime *= args.Scale;

        IncreaseTimer(status, statusLifetime);

        if (modified)
            EntitySystem.Get<SpecialModifierSystem>().RefreshClothingSpecialModifiers(args.SolutionEntity);

    }
    public void IncreaseTimer(CharismaModifierMetabolismComponent status, float time)
    {
        var gameTiming = IoCManager.Resolve<IGameTiming>();

        var offsetTime = Math.Max(status.ModifierTimer.TotalSeconds, gameTiming.CurTime.TotalSeconds);

        status.ModifierTimer = TimeSpan.FromSeconds(offsetTime + time);
        status.Dirty();
    }
}
