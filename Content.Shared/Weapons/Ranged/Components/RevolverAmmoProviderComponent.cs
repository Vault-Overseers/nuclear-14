using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed class RevolverAmmoProviderComponent : AmmoProviderComponent
{
    /*
     * Revolver has an array of its slots of which we can fire from any index.
     * We also keep a separate array of slots we haven't spawned entities for, Chambers. This means that rather than creating
     * for example 7 entities when revolver spawns (1 for the revolver and 6 cylinders) we can instead defer it.
     */

    [ViewVariables, DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    public Container AmmoContainer = default!;

    [ViewVariables, DataField("currentSlot")]
    public int CurrentIndex;

    [ViewVariables, DataField("capacity")]
    public int Capacity = 6;

    // Like BallisticAmmoProvider we defer spawning until necessary
    // AmmoSlots is the instantiated ammo and Chambers is the unspawned ammo (that may or may not have been shot).

    // TODO: Using an array would be better but this throws!
    [DataField("ammoSlots")]
    public List<EntityUid?> AmmoSlots = new();

    [DataField("chambers")]
    public bool?[] Chambers = Array.Empty<bool?>();

    [DataField("proto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FillPrototype = "CartridgeMagnum";

    [ViewVariables, DataField("soundEject")]
    public SoundSpecifier? SoundEject = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    [ViewVariables, DataField("soundInsert")]
    public SoundSpecifier? SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [ViewVariables, DataField("soundSpin")]
    public SoundSpecifier? SoundSpin = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/revolver_spin.ogg");
}
