using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public sealed class EmitterComponent : Component
    {
        public CancellationTokenSource? TimerCancel;

        // whether the power switch is in "on"
        [ViewVariables] public bool IsOn;
        // Whether the power switch is on AND the machine has enough power (so is actively firing)
        [ViewVariables] public bool IsPowered;

        // For the "emitter fired" sound
        public const float Variation = 0.25f;
        public const float Volume = 0.5f;
        public const float Distance = 6f;

        [ViewVariables] public int FireShotCounter;

        [ViewVariables] [DataField("fireSound")] public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Weapons/emitter.ogg");
        [ViewVariables] [DataField("boltType")] public string BoltType = "EmitterBolt";
        [ViewVariables] [DataField("powerUseActive")] public int PowerUseActive = 500;
        [ViewVariables] [DataField("fireBurstSize")] public int FireBurstSize = 3;
        [ViewVariables] [DataField("fireInterval")] public TimeSpan FireInterval = TimeSpan.FromSeconds(2);
        [ViewVariables] [DataField("fireBurstDelayMin")] public TimeSpan FireBurstDelayMin = TimeSpan.FromSeconds(2);
        [ViewVariables] [DataField("fireBurstDelayMax")] public TimeSpan FireBurstDelayMax = TimeSpan.FromSeconds(10);
    }
}
