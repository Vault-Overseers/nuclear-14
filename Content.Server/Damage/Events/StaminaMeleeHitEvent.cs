using Robust.Shared.Collections;
using Content.Server.Damage.Components;

namespace Content.Server.Damage.Events;

/// <summary>
/// The components in the list are going to be hit,
/// give opportunities to change the damage or other stuff.
/// </summary>
public sealed class StaminaMeleeHitEvent : HandledEntityEventArgs
{
    /// <summary>
    /// List of hit stamina components.
    public ValueList<StaminaComponent> HitList;

    /// <summmary>
    /// The multiplier. Generally, try to use *= or /= instead of overwriting.
    /// </summary>
    public float Multiplier = 1;

    /// <summary>
    /// The flat modifier. Generally, try to use += or -= instead of overwriting.
    /// </summary>
    public float FlatModifier = 0;

    public StaminaMeleeHitEvent(ValueList<StaminaComponent> hitList)
    {
        HitList = hitList;
    }
}
