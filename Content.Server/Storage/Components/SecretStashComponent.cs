using Content.Server.Storage.EntitySystems;
using Content.Server.Toilet;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Logic for a secret slot stash, like plant pot or toilet cistern.
    ///     Unlike <see cref="ItemSlotsComponent"/> it doesn't have interaction logic or verbs.
    ///     Other classes like <see cref="ToiletComponent"/> should implement it.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(SecretStashSystem))]
    public sealed class SecretStashComponent : Component
    {
        private string _secretPartName = string.Empty;

        /// <summary>
        ///     Max item size that can be fitted into secret stash.
        /// </summary>
        [ViewVariables] [DataField("maxItemSize")]
        public int MaxItemSize = (int) ReferenceSizes.Pocket;

        /// <summary>
        ///     IC secret stash name. For example "the toilet cistern".
        ///     If empty string, will replace it with entity name in init.
        /// </summary>
        [ViewVariables] [DataField("secretPartName", readOnly: true)]
        public string SecretPartName
        {
            get => _secretPartName;
            set => _secretPartName = Loc.GetString(value);
        }

        /// <summary>
        ///     Container used to keep secret stash item.
        /// </summary>
        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

    }
}
