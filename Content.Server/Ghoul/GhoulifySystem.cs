using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Radiation.Events;
using Content.Server.Polymorph.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.Ghoul;

public sealed partial class GhoulifySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhoulifyComponent, OnIrradiatedEvent>(OnIrradiated);
        SubscribeLocalEvent<GhoulifyComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<FeralGhoulifyComponent, OnIrradiatedEvent>(OnFeralIrradiated);
        SubscribeLocalEvent<FeralGhoulifyComponent, DamageChangedEvent>(OnFeralDamage);
    }

    private void OnIrradiated(EntityUid uid, GhoulifyComponent comp, OnIrradiatedEvent args)
    {
        if (args.TotalRads <= 0)
            return;

        comp.AccumulatedRads += args.TotalRads;
        if (comp.AccumulatedRads >= comp.NextNotify)
        {
            _popup.PopupEntity(Loc.GetString("ghoulify-start"), uid, uid);
            comp.NextNotify += comp.Threshold / 2f;
        }

        if (comp.AccumulatedRads < comp.Threshold)
            return;

        if (_random.Prob(args.TotalRads * comp.ProbabilityPerRad))
            Ghoulify(uid, comp);
    }

    private void OnDamage(EntityUid uid, GhoulifyComponent comp, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;
        if (args.DamageDelta.DamageDict.TryGetValue("Radiation", out var rad) && rad < 0)
            comp.AccumulatedRads = Math.Max(0f, comp.AccumulatedRads + (float) rad);
    }

    private void Ghoulify(EntityUid uid, GhoulifyComponent comp)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;
        if (humanoid.Species == "Ghoul" || humanoid.Species == "GhoulGlowing")
            return;

        _humanoid.SetSpecies(uid, "Ghoul", humanoid: humanoid);
        _popup.PopupEntity(Loc.GetString("ghoulify-complete"), uid, uid);
        RemComp<GhoulifyComponent>(uid);
        EnsureComp<FeralGhoulifyComponent>(uid);
    }

    private void OnFeralIrradiated(EntityUid uid, FeralGhoulifyComponent comp, OnIrradiatedEvent args)
    {
        if (args.TotalRads <= 0)
            return;

        comp.AccumulatedRads += args.TotalRads;
        if (comp.AccumulatedRads >= comp.NextNotify)
        {
            _popup.PopupEntity(Loc.GetString("ghoul-feral-start"), uid, uid);
            comp.NextNotify += comp.Threshold / 2f;
        }

        if (comp.AccumulatedRads < comp.Threshold)
            return;

        if (_random.Prob(args.TotalRads * comp.ProbabilityPerRad))
            Feralize(uid);
    }

    private void OnFeralDamage(EntityUid uid, FeralGhoulifyComponent comp, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;
        if (args.DamageDelta.DamageDict.TryGetValue("Radiation", out var rad) && rad < 0)
            comp.AccumulatedRads = Math.Max(0f, comp.AccumulatedRads + (float) rad);
    }

    private void Feralize(EntityUid uid)
    {
        var ent = _polymorph.PolymorphEntity(uid, "GhoulFeralPolymorph");
        if (ent != null)
        {
            _popup.PopupEntity(Loc.GetString("ghoul-feral-complete"), ent.Value, ent.Value);
            RemCompDeferred<FeralGhoulifyComponent>(uid);
        }
    }
}
