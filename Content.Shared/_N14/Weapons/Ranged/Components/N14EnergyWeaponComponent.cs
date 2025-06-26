using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._N14.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class N14EnergyWeaponComponent : AmmoProviderComponent
{
    /// <summary>Energy consumed per shot.</summary>
    [DataField("fireCost"), AutoNetworkedField]
    public float FireCost = 50f;

    /// <summary>If true the weapon uses hitscan, otherwise projectile.</summary>
    [DataField("hitscan"), AutoNetworkedField]
    public bool Hitscan = true;

    /// <summary>Projectile prototype to spawn when firing.</summary>
    [DataField("projectileProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), AutoNetworkedField]
    public string? ProjectileProto;

    /// <summary>Hitscan prototype to use when firing.</summary>
    [DataField("hitscanProto", customTypeSerializer: typeof(PrototypeIdSerializer<HitscanPrototype>)), AutoNetworkedField]
    public string? HitscanProto;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Shots;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Capacity;
}
