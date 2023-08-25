using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(WaveDefenseRuleSystem))]
public sealed class WaveDefenseRuleComponent : Component
{
    [DataField("difficulty")]
    public float DifficultyMod = 0.65f;

    [DataField("waveTime")]
    public int WaveTime = 240;

    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetSound = new SoundPathSpecifier("/Audio/Misc/nukeops.ogg");
}
