using Content.Shared.Storage;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    public sealed class TimedStorageFillComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("contents")]
        public List<EntitySpawnEntry> Contents = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("minimumSeconds")]
        public int MinimumSeconds = 300;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maximumSeconds")]
        public int MaximumSeconds = 1200;

        public float NextRefillTime = 0;
    }
}
