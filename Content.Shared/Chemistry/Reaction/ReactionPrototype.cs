using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Chemistry.Reaction
{
    /// <summary>
    /// Prototype for chemical reaction definitions
    /// </summary>
    [Prototype("reaction")]
    public sealed class ReactionPrototype : IPrototype, IComparable<ReactionPrototype>
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        /// Reactants required for the reaction to occur.
        /// </summary>
        [DataField("reactants", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<ReactantPrototype, ReagentPrototype>))]
        public Dictionary<string, ReactantPrototype> Reactants = new();

        /// <summary>
        ///     The minimum temperature the reaction can occur at.
        /// </summary>
        [DataField("minTemp")]
        public float MinimumTemperature = 0.0f;

        /// <summary>
        ///     The maximum temperature the reaction can occur at.
        /// </summary>
        [DataField("maxTemp")]
        public float MaximumTemperature = float.PositiveInfinity;

        /// <summary>
        /// Reagents created when the reaction occurs.
        /// </summary>
        [DataField("products", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
        public Dictionary<string, FixedPoint2> Products = new();

        /// <summary>
        /// Effects to be triggered when the reaction occurs.
        /// </summary>
        [DataField("effects", serverOnly: true)] public List<ReagentEffect> Effects = new();

        /// <summary>
        /// How dangerous is this effect? Stuff like bicaridine should be low, while things like methamphetamine
        /// or potas/water should be high.
        /// </summary>
        [DataField("impact", serverOnly: true)] public LogImpact Impact = LogImpact.Low;

        // TODO SERV3: Empty on the client, (de)serialize on the server with module manager is server module
        [DataField("sound", serverOnly: true)] public SoundSpecifier Sound { get; private set; } = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");

        /// <summary>
        /// If true, this reaction will only consume only integer multiples of the reactant amounts. If there are not
        /// enough reactants, the reaction does not occur. Useful for spawn-entity reactions (e.g. creating cheese).
        /// </summary>
        [DataField("quantized")] public bool Quantized = false;

        /// <summary>
        /// Determines the order in which reactions occur. This should used to ensure that (in general) descriptive /
        /// pop-up generating and explosive reactions occur before things like foam/area effects.
        /// </summary>
        [DataField("priority")]
        public int Priority;

        /// <summary>
        ///     Comparison for creating a sorted set of reactions. Determines the order in which reactions occur.
        /// </summary>
        public int CompareTo(ReactionPrototype? other)
        {
            if (other == null)
                return -1;

            if (Priority != other.Priority)
                return other.Priority - Priority;

            // Prioritize reagents that don't generate products. This should reduce instances where a solution
            // temporarily overflows and discards products simply due to the order in which the reactions occurred.
            // Basically: Make space in the beaker before adding new products.
            if (Products.Count != other.Products.Count)
                return Products.Count - other.Products.Count;

            return ID.CompareTo(other.ID);
        }
    }

    /// <summary>
    /// Prototype for chemical reaction reactants.
    /// </summary>
    [DataDefinition]
    public sealed class ReactantPrototype
    {
        [DataField("amount")]
        private FixedPoint2 _amount = FixedPoint2.New(1);
        [DataField("catalyst")]
        private bool _catalyst;

        /// <summary>
        /// Minimum amount of the reactant needed for the reaction to occur.
        /// </summary>
        public FixedPoint2 Amount => _amount;
        /// <summary>
        /// Whether or not the reactant is a catalyst. Catalysts aren't removed when a reaction occurs.
        /// </summary>
        public bool Catalyst => _catalyst;
    }
}
