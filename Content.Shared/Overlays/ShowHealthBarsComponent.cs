using Content.Shared.Damage.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Overlays;

/// <summary>
/// This component allows you to see health bars above damageable mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowHealthBarsComponent : Component
{
    /// <summary>
    /// Displays health bars of the damage containers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageContainerPrototype>> DamageContainers = new()
    {
        "Biological"
    };

    [DataField, AutoNetworkedField]
    public ProtoId<HealthIconPrototype>? HealthStatusIcon = "HealthIconFine";
}
