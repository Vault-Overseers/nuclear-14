using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using GameStateDelta = Robust.Shared.GameStates.IComponentDelta;

namespace Content.Shared.Weapons.Melee
{
    public sealed partial class MeleeWeaponComponent : ISerializationGenerated<GameStateDelta>
    {
        GameStateDelta ISerializationGenerated<GameStateDelta>.Instantiate()
        {
            return default!;
        }

        void ISerializationGenerated<GameStateDelta>.Copy(ref GameStateDelta value, ISerializationManager manager, SerializationHookContext context, ISerializationContext? copyContext)
        {
        }

        void ISerializationGenerated<GameStateDelta>.InternalCopy(ref GameStateDelta value, ISerializationManager manager, SerializationHookContext context, ISerializationContext? copyContext)
        {
        }
    }
}
