using System;
using System.Collections.Generic;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Server._N14.FEV.Components;

/// <summary>
/// Marks an entity as an FEV vat that injects FEV when someone is buckled into it.
/// </summary>
[RegisterComponent]
[Access(typeof(Systems.FEVVatSystem))]
public sealed partial class FEVVatComponent : Component
{
    /// <summary>Amount of FEV to inject if the victim exits early.</summary>
    [DataField("slowAmount")]
    public FixedPoint2 SlowAmount = FixedPoint2.New(5);

    /// <summary>Amount of FEV to inject for an instant transform.</summary>
    [DataField("instantAmount")]
    public FixedPoint2 InstantAmount = FixedPoint2.New(20);

    /// <summary>Time required in the vat for an instant mutation.</summary>
    [DataField("transformTime")]
    public float TransformTime = 3f;

    /// <summary>Tracked buckle start times for each victim.</summary>
    [DataField(ignore: true)]
    public Dictionary<EntityUid, TimeSpan> Buckled = new();
}
