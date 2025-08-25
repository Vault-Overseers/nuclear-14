using Robust.Shared.GameObjects;   // Component / IComponent
using Robust.Shared.Utility;       // ResPath
using System.Collections.Generic;

namespace Content.Shared._CP14.StationDungeonMap;

[RegisterComponent] // registers as CP14StationAdditionalMap
public sealed partial class CP14StationAdditionalMapComponent : Component
{
    [DataField("mapPaths")]
    public List<ResPath> MapPaths = new();
}
