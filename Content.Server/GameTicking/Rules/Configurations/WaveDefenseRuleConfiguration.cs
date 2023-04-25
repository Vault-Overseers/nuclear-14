using Content.Server.GameTicking.Rules.Configurations;
using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Configurations;

public sealed class WaveDefenseRuleConfiguration : GameRuleConfiguration
{
    public override string Id => "WaveDefense";

    [DataField("difficulty")]
    public float DifficultyMod = 0.8f;

    [DataField("waveTime")]
    public int WaveTime = 420;

    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetSound = new SoundPathSpecifier("/Audio/Misc/nukeops.ogg");
}
