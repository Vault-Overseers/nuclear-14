using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NC.Roles
{
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class JobBlockAbstract { }

    [UsedImplicitly]
    public sealed partial class JobBlockForSpecie : JobBlockAbstract
    {
        [DataField("nameSpecie")]
        public string? NameSpecie { get; set; }

        [DataField("inverted")]
        public bool Inverted { get; set; }
    }

    public static class JobBlockForSpecies
    {
        public static bool TryRequirementMet(
            JobBlockAbstract jobBlockAbstract,
            string species,
            [NotNullWhen(false)] out string? reason)
        {
            reason = null;

            if (jobBlockAbstract is not JobBlockForSpecie jobBlockForSpecie)
            {
                reason = string.Empty;
                return false;
            }

            if (jobBlockForSpecie.Inverted)
            {
                return true;
            }

            if (string.Equals(jobBlockForSpecie.NameSpecie, species, StringComparison.OrdinalIgnoreCase))
            {
                reason = Loc.GetString("role-timer-race-ban");
                return false;
            }

            return true;
        }
    }
}