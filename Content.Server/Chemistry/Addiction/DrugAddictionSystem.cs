using Content.Server.StatusEffects;
using Content.Server.Jittering;
using Content.Server.Medical;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Maths;

namespace Content.Server.Chemistry.Addiction;

public sealed class DrugAddictionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var tolQuery = EntityQueryEnumerator<DrugToleranceComponent>();
        while (tolQuery.MoveNext(out var uid, out var comp))
        {
            var keys = comp.Tolerances.Keys.ToList();
            foreach (var key in keys)
            {
                var value = comp.Tolerances[key];
                var decay = frameTime / (60f * (1f + value / 5f));
                value -= decay;
                if (value <= 0f)
                    comp.Tolerances.Remove(key);
                else
                    comp.Tolerances[key] = value;
            }
        }

        var addQuery = EntityQueryEnumerator<AddictedComponent>();
        while (addQuery.MoveNext(out var uid, out var add))
        {
            foreach (var drug in add.Addictions.ToArray())
            {
                add.LastUse.TryGetValue(drug, out var last);
                var lastTime = TimeSpan.FromSeconds(last);
                if (_timing.CurTime - lastTime > TimeSpan.FromMinutes(5))
                {
                    if (_status.TryAddStatusEffect(uid, drug, TimeSpan.FromSeconds(1), true))
                    {
                        _jitter.DoJitter(uid, TimeSpan.FromSeconds(1), false);
                        if (_rand.Prob(0.05f * frameTime))
                            _vomit.Vomit(uid);
                    }
                }
                else
                {
                    _status.TryRemoveStatusEffect(uid, drug);
                }
            }
        }
    }

    public float GetTolerance(EntityUid uid, string id)
    {
        if (!TryComp(uid, out DrugToleranceComponent? comp) || !comp.Tolerances.TryGetValue(id, out var tol))
            return 0f;
        return tol;
    }

    public float GetEffectScale(EntityUid uid, string id)
    {
        var tolerance = GetTolerance(uid, id);
        return MathHelper.Clamp(1f / (1f + tolerance / 5f), 0.25f, 1f);
    }

    public void OnDrugUsed(EntityUid uid, string id, float chance, float toleranceGain, string status)
    {
        var tol = EnsureComp<DrugToleranceComponent>(uid);
        tol.Tolerances.TryGetValue(id, out var value);
        value += toleranceGain;
        tol.Tolerances[id] = value;
        Dirty(uid, tol);

        var addicted = EnsureComp<AddictedComponent>(uid);
        addicted.LastUse[id] = (float) _timing.CurTime.TotalSeconds;
        if (!addicted.Addictions.Contains(id))
        {
            var effective = MathHelper.Clamp(chance * (1f + value / 5f), 0f, 0.8f);
            if (_rand.Prob(effective))
                addicted.Addictions.Add(id);
        }
        Dirty(uid, addicted);
        _status.TryRemoveStatusEffect(uid, status);
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DrugToleranceComponent, TryMetabolizeReagent>(OnTryMetabolize);
    }

    private void OnTryMetabolize(EntityUid uid, DrugToleranceComponent comp, ref TryMetabolizeReagent args)
    {
        args.Scale *= GetEffectScale(uid, args.Prototype.ID);
    }

    public void CureAddiction(EntityUid uid, string id)
    {
        if (!TryComp(uid, out AddictedComponent? add))
            return;

        add.Addictions.Remove(id);
        add.LastUse.Remove(id);
        Dirty(uid, add);

        _status.TryRemoveStatusEffect(uid, $"{id}Addiction");
    }

    public void CureAllAddictions(EntityUid uid)
    {
        if (!TryComp(uid, out AddictedComponent? add))
            return;

        foreach (var drug in add.Addictions.ToArray())
            CureAddiction(uid, drug);
    }
}
