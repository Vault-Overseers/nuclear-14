using Content.Shared.Whitelist;

namespace Content.Server._NC.NPCTaming;

[RegisterComponent]
// ReSharper disable once InconsistentNaming
public sealed partial class NPCTamingOnTouchBehaviourComponent : Component
{
    /// <summary>
    /// Whitelist to determine which entities can tame this pet, if set to null - all entities can
    /// </summary>
    [DataField, ViewVariables]
    public EntityWhitelist? Whitelist = null;

    /// <summary>
    /// If other player is able to "retame" this pet
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Persistent;

    /// <summary>
    /// If player has one try to tame this pet
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool OneTry;

    /// <summary>
    /// List with players which has already tried to tame this pet, works with <see cref="OneTry"/>
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> TriedPlayers = new();

    /// <summary>
    /// Should be self-explanatory
    /// </summary>
    [DataField, ViewVariables]
    public float TameChance;

    /// <summary>
    /// Should this pet follow his friend, true by default
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Follow = true;

    /// <summary>
    /// Successfully tamed popup message
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string SuccessPopup = string.Empty;

    /// <summary>
    /// Failed to tame popup message
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string DeniedPopup = string.Empty;

    /// <summary>
    /// Current friend
    /// </summary>
    [ViewVariables]
    public EntityUid? Friend = null;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AggroTime = TimeSpan.FromSeconds(20);

    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> AggroMemories = new();
}
