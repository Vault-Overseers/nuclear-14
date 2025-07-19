namespace Content.Server._CP14.Temperature;

/// <summary>
/// passively returns the solution temperature to the standard
/// </summary>
[RegisterComponent, Access(typeof(CP14TemperatureSystem))]
public sealed partial class CP14SolutionTemperatureComponent : Component
{
    [DataField]
    public float StandardTemp = 300f;
}
