using Content.Server.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [Access(typeof(DamageOtherOnHitSystem))]
    [RegisterComponent]
    public sealed partial class DamageOtherOnHitComponent : Component
    {
        //
        /// <summary>
        ///     N14: left to exclude conflicts, in fact can be implemented through IgnoreCoefficients
        /// </summary>
        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances = false;

        /// <summary>
        ///     N14: Allows you to ignore armor protection
        /// </summary>
        [DataField("ignoreCoefficients")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float IgnoreCoefficients = 0f;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

    }
}
