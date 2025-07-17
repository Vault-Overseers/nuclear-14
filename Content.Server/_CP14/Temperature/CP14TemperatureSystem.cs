using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._CP14.Temperature;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Placeable;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._CP14.Temperature;

public sealed partial class CP14TemperatureSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private readonly TimeSpan _updateTick = TimeSpan.FromSeconds(1f);
    private TimeSpan _timeToNextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CP14TemperatureTransformationComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
        SubscribeLocalEvent<CP14TemperatureTransmissionComponent, OnTemperatureChangeEvent>(OnTemperatureTransmite);
    }

    /// <summary>
    /// The main idea is that we do not simulate the interaction between the temperature of the container and its contents.
    /// We directly change the temperature of the entire contents of the container.
    /// </summary>
    private void OnTemperatureTransmite(Entity<CP14TemperatureTransmissionComponent> ent,
        ref OnTemperatureChangeEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var heatAmount = args.TemperatureDelta * _temperature.GetHeatCapacity(ent);

        // copy the list to avoid modifying it while iterating
        var containedEntities = container.ContainedEntities.ToList();

        var entityCount = containedEntities.Count;
        foreach (var contained in containedEntities)
        {
            _temperature.ChangeHeat(contained, heatAmount / entityCount);
        }
    }

    private void OnTemperatureChanged(Entity<CP14TemperatureTransformationComponent> start,
        ref OnTemperatureChangeEvent args)
    {
        var xform = Transform(start);
        foreach (var entry in start.Comp.Entries)
        {
            if (args.CurrentTemperature > entry.TemperatureRange.X &&
                args.CurrentTemperature < entry.TemperatureRange.Y)
            {
                if (entry.TransformTo == null)
                    continue;

                SpawnNextToOrDrop(entry.TransformTo, start);
                Del(start);

                break;
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime <= _timeToNextUpdate)
            return;

        _timeToNextUpdate = _timing.CurTime + _updateTick;

        FlammableEntityHeating();
        FlammableSolutionHeating();
        NormalizeSolutionTemperature();
    }

    private float GetTargetTemperature(FlammableComponent flammable, CP14FlammableSolutionHeaterComponent heater)
    {
        return flammable.FireStacks * heater.DegreesPerStack;
    }

    private void NormalizeSolutionTemperature()
    {
        var query = EntityQueryEnumerator<CP14SolutionTemperatureComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var temp, out var container))
        {
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((uid, container)))
            {
                if (TryAffectTemp(soln.Comp.Solution.Temperature,
                        temp.StandardTemp,
                        soln.Comp.Solution.Volume,
                        out var newT,
                        power: 0.05f))
                    _solutionContainer.SetTemperature(soln, newT);
            }
        }
    }

    private void FlammableEntityHeating()
    {
        var flammableQuery =
            EntityQueryEnumerator<CP14FlammableEntityHeaterComponent, ItemPlacerComponent, FlammableComponent>();
        while (flammableQuery.MoveNext(out _, out var heater, out var itemPlacer, out var flammable))
        {
            if (!flammable.OnFire)
                continue;

            var energy = flammable.FireStacks * heater.DegreesPerStack;
            foreach (var ent in itemPlacer.PlacedEntities)
            {
                _temperature.ChangeHeat(ent, energy);
            }
        }
    }

    private void FlammableSolutionHeating()
    {
        var query =
            EntityQueryEnumerator<CP14FlammableSolutionHeaterComponent, ItemPlacerComponent, FlammableComponent>();
        while (query.MoveNext(out _, out var heater, out var itemPlacer, out var flammable))
        {
            if (!flammable.OnFire)
                continue;

            foreach (var heatingEntity in itemPlacer.PlacedEntities)
            {
                if (!TryComp<SolutionContainerManagerComponent>(heatingEntity, out var container))
                    continue;

                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((heatingEntity, container)))
                {
                    if (TryAffectTemp(soln.Comp.Solution.Temperature,
                            GetTargetTemperature(flammable, heater),
                            soln.Comp.Solution.Volume,
                            out var newT))
                        _solutionContainer.SetTemperature(soln, newT);
                }
            }
        }
    }

    private static bool TryAffectTemp(float oldT, float targetT, FixedPoint2 mass, out float newT, float power = 1)
    {
        newT = oldT;

        if (mass == 0)
            return false;

        newT = (float)(oldT + (targetT - oldT) / mass * power);
        return true;
    }
}
