﻿using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Prototypes;

/// <summary>
/// This is a prototype for categorizing
/// different types of machine parts.
/// </summary>
[Prototype("machinePart")]
public sealed class MachinePartPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// A stock part entity based on the machine part.
    /// </summary>
    [DataField("stockPartPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StockPartPrototype { get; }
}
