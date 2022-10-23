namespace Content.Server.Warps
{
    /// <summary>
    /// Allows ghosts etc to warp to this entity by name.
    /// </summary>
    [RegisterComponent]
    public sealed class WarpPointComponent : Component
    {
        /// Unique (across all loaded maps) identifier for teleporting to warp points.
        [ViewVariables(VVAccess.ReadWrite)] [DataField("id")] public string? ID { get; set; }

        /// Readable name for ghost warp points.
        [ViewVariables(VVAccess.ReadWrite)] [DataField("location")] public string? Location { get; set; }

        /// <summary>
        ///     If true, ghosts warping to this entity will begin following it.
        /// </summary>
        [DataField("follow")]
        public readonly bool Follow = false;
    }
}
