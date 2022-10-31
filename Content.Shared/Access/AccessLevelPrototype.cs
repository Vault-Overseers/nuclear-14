using Robust.Shared.Prototypes;

namespace Content.Shared.Access
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype("accessLevel")]
    public sealed class AccessLevelPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        ///     The player-visible name of the access level, in the ID card console and such.
        /// </summary>
        [DataField("name")]
        public string Name
        {
            get => (_name is not null) ? _name : ID;
            private set => _name = Loc.GetString(value);
        }

        private string? _name;
    }
}
