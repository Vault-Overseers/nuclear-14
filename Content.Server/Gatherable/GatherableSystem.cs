using Content.Server.Destructible;
using Content.Server.DoAfter;
using Content.Server.Gatherable.Components;
using Content.Shared.DoAfter;
using Content.Shared.EntityList;
using Content.Shared.Gatherable;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GatherableComponent, InteractUsingEvent>(OnToolActivate);
        SubscribeLocalEvent<GatherableComponent, InteractHandEvent>(OnHandActivate);
        SubscribeLocalEvent<GatherableComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<GatherableComponent, GatherableDoAfterEvent>(OnDoAfterGather);

        InitializeProjectile();
    }

    private void OnAttacked(EntityUid uid, GatherableComponent component, AttackedEvent args)
    {
        if (component.ToolWhitelist?.IsValid(args.Used, EntityManager) != true || component.GatherTime != 0)
            return;

        Gather(uid, args.User, args.Used, component);
    }

    private void OnToolActivate(EntityUid uid, GatherableComponent component, InteractUsingEvent args)
    {
        if (component.ToolWhitelist?.IsValid(args.Used, EntityManager) != true)
            return;

        //TODO: integrate the tools qualities the right way
        var gatherEvent = new GatherableDoAfterEvent();

        if (TryComp<ToolComponent>(args.Used, out var tool))
        {
            _toolSystem.UseTool(args.Used, args.User, uid, component.GatherTime, tool.Qualities, gatherEvent);
        }
        else
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(args.User, component.GatherTime, gatherEvent, uid, uid,
                args.Used)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                MovementThreshold = 1.0f,
            });
        }
    }
    private void OnHandActivate(EntityUid uid, GatherableComponent component, InteractHandEvent args)
    {
        if (component.ToolWhitelist?.IsValid(args.User, EntityManager) != true)
            return;

        var gatherEvent = new GatherableDoAfterEvent();

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(args.User, component.GatherTime, gatherEvent, uid, uid)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 1.0f,
        });
    }

    private void OnDoAfterGather(EntityUid uid, GatherableComponent component, GatherableDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        Gather(uid, args.User, args.Used, component);
    }
    public void Gather(EntityUid gatheredUid, EntityUid? gatherer = null, EntityUid? tool = null, GatherableComponent? component = null, SoundSpecifier? sound = null)
    {
        if (!Resolve(gatheredUid, ref component))
            return;

        // Complete the gathering process
        _destructible.DestroyEntity(gatheredUid);
        _audio.PlayPvs(sound, gatheredUid);

        // Spawn the loot!
        if (component.MappedLoot == null)
            return;

        var pos = Transform(gatheredUid).MapPosition;

        foreach (var (tag, table) in component.MappedLoot)
        {
            if (tag != "All")
            {
                if ((gatherer != null && tool != null && !_tagSystem.HasTag(tool.Value, tag)) || tool == null)
                    continue;
            }
            var getLoot = _prototypeManager.Index<EntityLootTablePrototype>(table);
            var spawnLoot = getLoot.GetSpawns();
            var spawnPos = pos.Offset(_random.NextVector2(0.3f));
            foreach (var loot in spawnLoot)
            {
                Spawn(loot, spawnPos);
            }
        }
    }
}



