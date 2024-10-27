using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;

namespace Content.Shared.Customization.Systems;

/// <summary>
/// Checking for a list of prohibited species
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class JobBlockForSpeciesRequirement : CharacterRequirement
{
    [DataField(required: true)]
    public string[] Species { get; set; } = Array.Empty<string>();

    public override bool IsValid(JobPrototype job, HumanoidCharacterProfile profile,
        Dictionary<string, TimeSpan> playTimes, bool whitelisted, IPrototype prototype,
        IEntityManager entityManager, IPrototypeManager prototypeManager, IConfigurationManager configManager,
        out FormattedMessage? reason, int depth = 0)
    {
        if (Species.Any(species => string.Equals(species, profile.Species, StringComparison.OrdinalIgnoreCase)))
        {
            reason = FormattedMessage.FromMarkup(Loc.GetString("role-timer-race-ban"));
            return false;
        }

        reason = null;
        return true;
    }
}