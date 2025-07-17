namespace Content.Server._CP14.Temperature;

/// <summary>
/// when it receives the temperature, it distributes it among all objects inside the specified container
/// </summary>
[RegisterComponent, Access(typeof(CP14TemperatureSystem))]
public sealed partial class CP14TemperatureTransmissionComponent : Component
{
    [DataField("containerId", required: true)]
    public string ContainerId = string.Empty;
}
