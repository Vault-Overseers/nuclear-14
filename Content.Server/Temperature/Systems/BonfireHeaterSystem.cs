using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Placeable;
using Content.Shared.Temperature;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles <see cref="BonfireHeaterComponent"/> updating and events.
/// </summary>
public sealed class BonfireHeaterSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<BonfireHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var comp, out var placer))
        {
            var heatToAdd = comp.BaseHeatMultiplier;
            foreach (var ent in placer.PlacedEntities)
            {
                _temperature.ChangeHeat(ent, heatToAdd, true);
            }
        }
    }
}
