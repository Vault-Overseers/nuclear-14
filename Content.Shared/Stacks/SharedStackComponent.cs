using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Stacks
{
    [NetworkedComponent, Access(typeof(SharedStackSystem))]
    public abstract class SharedStackComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("stackType", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string StackTypeId { get; private set; } = string.Empty;

        /// <summary>
        ///     Current stack count.
        ///     Do NOT set this directly, use the <see cref="SharedStackSystem.SetCount"/> method instead.
        /// </summary>
        [DataField("count")]
        public int Count { get; set; } = 30;

        /// <summary>
        ///     Max amount of things that can be in the stack.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("max")]
        public int MaxCount { get; set; } = 30;

        /// <summary>
        ///     Set to true to not reduce the count when used.
        /// </summary>
        [DataField("unlimited")]
        [ViewVariables(VVAccess.ReadOnly)]
        public bool Unlimited { get; set; }

        [ViewVariables]
        public int AvailableSpace => MaxCount - Count;
    }

    [Serializable, NetSerializable]
    public sealed class StackComponentState : ComponentState
    {
        public int Count { get; }
        public int MaxCount { get; }

        public StackComponentState(int count, int maxCount)
        {
            Count = count;
            MaxCount = maxCount;
        }
    }
}
