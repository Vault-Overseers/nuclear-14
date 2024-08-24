using Content.Server.Temperature.Systems;
using Content.Shared.Temperature;

namespace Content.Server.Temperature.Components;

/// <summary>
/// Adds thermal energy to entities with <see cref="TemperatureComponent"/> placed on it.
/// </summary>
[RegisterComponent, Access(typeof(BonfireHeaterSystem))]
public sealed partial class BonfireHeaterComponent : Component
{
    [DataField("baseHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseHeatMultiplier = 50;
}
