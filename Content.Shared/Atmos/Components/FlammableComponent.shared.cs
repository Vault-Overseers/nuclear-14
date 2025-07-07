using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Shared partial for <see cref="Content.Server.Atmos.Components.FlammableComponent"/>
/// so systems in shared code can reference it.
/// </summary>
[RegisterComponent]
public sealed partial class FlammableComponent : Component
{
    [DataField]
    public bool Resisting;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool OnFire;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float FireStacks;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaximumFireStacks = 10f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MinimumFireStacks = -10f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string FlammableFixtureID = "flammable";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MinIgnitionTemperature = 373.15f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool FireSpread { get; private set; } = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool CanResistFire { get; private set; } = false;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField]
    public IPhysShape FlammableCollisionShape = new PhysShapeCircle(0.35f);

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool AlwaysCombustible = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool CanExtinguish = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool IgnoreFireProtection = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float FirestacksOnIgnite = 2.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float FirestackFade = -0.1f;

    [DataField]
    public float FireStackIncreaseMultiplier = 1f;

    [DataField]
    public ProtoId<AlertPrototype> FireAlert = "Fire";
}
