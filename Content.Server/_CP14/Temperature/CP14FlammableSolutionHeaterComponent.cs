namespace Content.Server._CP14.Temperature;

/// <summary>
/// allows you to heat the temperature of solutions depending on the number of stacks of fire
/// </summary>
[RegisterComponent, Access(typeof(CP14TemperatureSystem))]
public sealed partial class CP14FlammableSolutionHeaterComponent : Component
{
    [DataField]
    public float DegreesPerStack = 100f;
}
