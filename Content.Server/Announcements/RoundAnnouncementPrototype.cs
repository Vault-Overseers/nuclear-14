using Content.Server.GameTicking.Presets;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Announcements;

/// <summary>
/// Used for any announcements on the start of a round.
/// </summary>
[Prototype("roundAnnouncement")]
public sealed class RoundAnnouncementPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [DataField("sound")] public SoundSpecifier? Sound;

    [DataField("message")] public string? Message;

    [DataField("presets", customTypeSerializer: typeof(PrototypeIdListSerializer<GamePresetPrototype>))]
    public List<string> GamePresets = new();
}
