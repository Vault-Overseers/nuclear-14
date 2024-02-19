// NOTE: NOT USED CURRENTLY, BUT MAYBE NEEDED LATER? REMOVE/USE/CHANGE BEFORE CHANGING DRAFT STATUS!

// using Content.Shared.Nuclear14.Special.Components;


// namespace Content.Shared.Nuclear.Special.Strength.EntitySystems;

// public sealed class StrengthSystem : EntitySystem
// {

//     [Dependency] private readonly IEntityManager _entity = default!;

//     public StrengthSystem()
//     {
//     }

//     /// <summary>
//     ///     Tries to add to the Strength level of player.
//     /// </summary>
//     /// <param name="uid">The entity uid.</param>
//     /// <param name="amount">The amount to add to the strength level.</param>
//     public bool TryAddStrengthAmount(EntityUid uid, int amount)
//     {
//         // Check if the entity has a Special Component
//         if (!_entity.TryGetComponent<SpecialComponent>(uid, out _))
//             return false;

//         // Set the new power level
//         AddStrengthAmount(uid, amount);

//         return true;
//     }

//     /// <summary>
//     ///     Adds to the strength level.
//     /// </summary>
//     /// <param name="uid">The entity uid.</param>
//     /// <param name="amount">The amount to add to the power level.</param>
//     public void AddStrengthAmount(EntityUid uid, int amount)
//     {
//         // Get Strength component
//         if (!_entity.TryGetComponent<SpecialComponent>(uid, out var component))
//         {
//             Logger.Error("Tried to add to strength level of entity without Special component.");
//             return;
//         }

//         // Get new power level
//         var newStrengthAmount = component.BaseStrength + amount;

//         // Clamp power level using clamp function
//         newStrengthAmount = Math.Clamp(newStrengthAmount, component.SpecialAmountMin, component.SpecialAmountMax);

//         // Set the new power level
//         SetStrengthAmount(uid, newStrengthAmount);
//     }


//     /// <summary>
//     ///     Sets the strength level of a player.
//     /// </summary>
//     /// <param name="uid">The entity uid.</param>
//     /// <param name="newPowerLevel">The new strength level.</param>
//     public void SetStrengthAmount(EntityUid uid, int newStrengthAmount)
//     {
//         // Get Strength component
//         if (!_entity.TryGetComponent<SpecialComponent>(uid, out var component))
//         {
//             Logger.Error("Tried to set Strength level of entity without Strength component.");
//             return;
//         }

//         // Clamp strength level using clamp function to not have less than 1 or more than 10
//         newStrengthAmount = Math.Clamp(newStrengthAmount, component.SpecialAmountMin, component.SpecialAmountMax);

//         // Set the new strength level
//         component.BaseStrength = newStrengthAmount;
//     }

// }
