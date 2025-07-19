using Content.Server.Temperature.Systems;

namespace Content.Server._CP14.Temperature;

/// <summary>
/// Adds thermal energy from FlammableComponent to entities with <see cref="TemperatureComponent"/> placed on it.
/// </summary>
[RegisterComponent, Access(typeof(EntityHeaterSystem))]
public sealed partial class CP14FlammableEntityHeaterComponent : Component
{
    [DataField]
    public float DegreesPerStack = 300f;
}
