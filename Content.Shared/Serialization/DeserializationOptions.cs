namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Minimal replacement for RobustToolbox deserialization options used during
    /// automated testing. Only the fields referenced in content are included.
    /// </summary>
    public struct DeserializationOptions
    {
        public static DeserializationOptions Default => new();

        public bool InitializeMaps;
        public bool PauseMaps;
        public bool StoreYamlUids;
    }
}
