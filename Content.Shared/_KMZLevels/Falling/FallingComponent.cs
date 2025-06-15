using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._KMZLevels.Falling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FallingComponent : Component
{
    /// <summary>
    /// How many seconds the mob will be paralyzed for.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LandingStunTime = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public float DamageModifier = 1f;

    /// <summary>
    ///  Base damage from falling. It can be increase by falling distance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier BaseDamage = new DamageSpecifier();

    /// <summary>
    /// Do not deal damage on object. But it can deal damage for collided objects on bottom levels.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreDamage;
}
