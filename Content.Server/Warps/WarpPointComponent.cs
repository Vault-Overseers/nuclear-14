namespace Content.Server.Warps
{
    /// <summary>
    /// Allows ghosts etc to warp to this entity by name.
    /// </summary>
    [RegisterComponent]
    public sealed partial class WarpPointComponent : Component
    {

        /// Unique (across all loaded maps) identifier for teleporting to warp points.
        [ViewVariables(VVAccess.ReadWrite)] [DataField("id")] public string? ID { get; set; }
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public string? Location;

        /// <summary>
        ///     If true, ghosts warping to this entity will begin following it.
        /// </summary>
        [DataField]
        public bool Follow;
    }
}
