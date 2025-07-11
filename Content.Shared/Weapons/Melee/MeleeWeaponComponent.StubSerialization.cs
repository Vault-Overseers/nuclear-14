using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee
{
    public sealed partial class MeleeWeaponComponent : ISerializationGenerated<IComponentDelta>
    {
        IComponentDelta ISerializationGenerated<IComponentDelta>.Instantiate()
        {
            return default!;
        }

        void ISerializationGenerated<IComponentDelta>.Copy(ref IComponentDelta value, ISerializationManager manager, SerializationHookContext context, ISerializationContext? copyContext)
        {
        }

        void ISerializationGenerated<IComponentDelta>.InternalCopy(ref IComponentDelta value, ISerializationManager manager, SerializationHookContext context, ISerializationContext? copyContext)
        {
        }
    }
}
