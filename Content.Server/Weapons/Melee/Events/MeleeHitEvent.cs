using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Weapons.Melee.Events;

/// <summary>
///     Raised directed on the melee weapon entity used to attack something in combat mode,
///     whether through a click attack or wide attack.
/// </summary>
public sealed class MeleeHitEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The base amount of damage dealt by the melee hit.
    /// </summary>
    public readonly DamageSpecifier BaseDamage = new();

    /// <summary>
    ///     Modifier sets to apply to the hit event when it's all said and done.
    ///     This should be modified by adding a new entry to the list.
    /// </summary>
    public List<DamageModifierSet> ModifiersList = new();

    /// <summary>
    ///     Damage to add to the default melee weapon damage. Applied before modifiers.
    /// </summary>
    /// <remarks>
    ///     This might be required as damage modifier sets cannot add a new damage type to a DamageSpecifier.
    /// </remarks>
    public DamageSpecifier BonusDamage = new();

    /// <summary>
    ///     A list containing every hit entity. Can be zero.
    /// </summary>
    public IEnumerable<EntityUid> HitEntities { get; }

    /// <summary>
    ///     Used to define a new hit sound in case you want to override the default GenericHit.
    ///     Also gets a pitch modifier added to it.
    /// </summary>
    public SoundSpecifier? HitSoundOverride {get; set;}

    /// <summary>
    /// The user who attacked with the melee weapon.
    /// </summary>
    public EntityUid User { get; }

    public MeleeHitEvent(List<EntityUid> hitEntities, EntityUid user, DamageSpecifier baseDamage)
    {
        HitEntities = hitEntities;
        User = user;
        BaseDamage = baseDamage;
    }
}