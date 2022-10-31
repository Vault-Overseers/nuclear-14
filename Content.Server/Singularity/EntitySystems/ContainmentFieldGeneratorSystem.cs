﻿using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, StartCollideEvent>(HandleGeneratorCollide);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ReAnchorEvent>(OnReanchorEvent);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var generator in EntityQuery<ContainmentFieldGeneratorComponent>())
        {
            if (generator.PowerBuffer <= 0) //don't drain power if there's no power, or if it's somehow less than 0.
                continue;

            generator.Accumulator += frameTime;

            if (generator.Accumulator >= generator.Threshold)
            {
                LosePower(generator.PowerLoss, generator);
                generator.Accumulator -= generator.Threshold;
            }
        }
    }

    #region Events

    /// <summary>
    /// A generator receives power from a source colliding with it.
    /// </summary>
    private void HandleGeneratorCollide(EntityUid uid, ContainmentFieldGeneratorComponent component, ref StartCollideEvent args)
    {
        if (_tags.HasTag(args.OtherFixture.Body.Owner, component.IDTag))
        {
            ReceivePower(component.PowerReceived, component);
            component.Accumulator = 0f;
        }
    }

    private void OnExamine(EntityUid uid, ContainmentFieldGeneratorComponent component, ExaminedEvent args)
    {
        if (component.Enabled)
            args.PushMarkup(Loc.GetString("comp-containment-on"));

        else
            args.PushMarkup(Loc.GetString("comp-containment-off"));
    }

    private void OnInteract(EntityUid uid, ContainmentFieldGeneratorComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(component.Owner, out TransformComponent? transformComp) && transformComp.Anchored)
        {
            if (!component.Enabled)
                TurnOn(component);
            else if (component.Enabled && component.IsConnected)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-containment-toggle-warning"), args.User, Filter.Entities(args.User), PopupType.LargeCaution);
                return;
            }
            else
                TurnOff(component);
        }
        args.Handled = true;
    }

    private void OnAnchorChanged(EntityUid uid, ContainmentFieldGeneratorComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            RemoveConnections(component);
    }

    private void OnReanchorEvent(EntityUid uid, ContainmentFieldGeneratorComponent component, ref ReAnchorEvent args)
    {
        GridCheck(component);
    }

    private void OnUnanchorAttempt(EntityUid uid, ContainmentFieldGeneratorComponent component,
        UnanchorAttemptEvent args)
    {
        if (component.Enabled)
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-containment-anchor-warning"), args.User, Filter.Entities(args.User), PopupType.LargeCaution);
            args.Cancel();
        }
    }

    private void TurnOn(ContainmentFieldGeneratorComponent component)
    {
        component.Enabled = true;
        ChangeFieldVisualizer(component);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-on"), component.Owner, Filter.Pvs(component.Owner));
    }

    private void TurnOff(ContainmentFieldGeneratorComponent component)
    {
        component.Enabled = false;
        ChangeFieldVisualizer(component);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-off"), component.Owner, Filter.Pvs(component.Owner));
    }

    private void OnComponentRemoved(EntityUid uid, ContainmentFieldGeneratorComponent component, ComponentRemove args)
    {
        RemoveConnections(component);
    }

    /// <summary>
    /// Deletes the fields and removes the respective connections for the generators.
    /// </summary>
    private void RemoveConnections(ContainmentFieldGeneratorComponent component)
    {
        foreach (var (direction, value) in component.Connections)
        {
            foreach (var field in value.Item2)
            {
                QueueDel(field);
            }
            value.Item1.Connections.Remove(direction.GetOpposite());

            if (value.Item1.Connections.Count == 0) //Change isconnected only if there's no more connections
            {
                value.Item1.IsConnected = false;
                ChangeOnLightVisualizer(value.Item1);
            }

            ChangeFieldVisualizer(value.Item1);
        }
        component.Connections.Clear();
        component.IsConnected = false;
        ChangeOnLightVisualizer(component);
        ChangeFieldVisualizer(component);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-disconnected"), component.Owner, Filter.Pvs(component.Owner), PopupType.LargeCaution);
    }

    #endregion

    #region Connections

    /// <summary>
    /// Stores power in the generator. If it hits the threshold, it tries to establish a connection.
    /// </summary>
    /// <param name="power">The power that this generator received from the collision in <see cref="HandleGeneratorCollide"/></param>
    public void ReceivePower(int power, ContainmentFieldGeneratorComponent component)
    {
        component.PowerBuffer += power;

        var genXForm = Transform(component.Owner);

        if (component.PowerBuffer >= component.PowerMinimum)
        {
            var directions = Enum.GetValues<Direction>().Length;
            for (int i = 0; i < directions-1; i+=2)
            {
                var dir = (Direction)i;

                if (component.Connections.ContainsKey(dir))
                    continue; // This direction already has an active connection

                TryGenerateFieldConnection(dir, component, genXForm);
            }
        }

        ChangePowerVisualizer(power, component);
    }

    public void LosePower(int power, ContainmentFieldGeneratorComponent component)
    {
        component.PowerBuffer -= power;

        if (component.PowerBuffer < component.PowerMinimum && component.Connections.Count != 0)
        {
            RemoveConnections(component);
        }

        ChangePowerVisualizer(power, component);
    }

    /// <summary>
    /// This will attempt to establish a connection of fields between two generators.
    /// If all the checks pass and fields spawn, it will store this connection on each respective generator.
    /// </summary>
    /// <param name="dir">The field generator establishes a connection in this direction.</param>
    /// <param name="component">The field generator component</param>
    /// <param name="gen1XForm">The transform component for the first generator</param>
    /// <returns></returns>
    private bool TryGenerateFieldConnection(Direction dir, ContainmentFieldGeneratorComponent component, TransformComponent gen1XForm)
    {
        if (!component.Enabled)
            return false;

        if (!gen1XForm.Anchored)
            return false;

        var genWorldPosRot = gen1XForm.GetWorldPositionRotation();
        var dirRad = dir.ToAngle() + genWorldPosRot.WorldRotation; //needs to be like this for the raycast to work properly

        var ray = new CollisionRay(genWorldPosRot.WorldPosition, dirRad.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(gen1XForm.MapID, ray, component.MaxLength, component.Owner, false);
        var genQuery = GetEntityQuery<ContainmentFieldGeneratorComponent>();

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (genQuery.HasComponent(result.HitEntity))
                closestResult = result;

            break;
        }
        if (closestResult == null)
            return false;

        var ent = closestResult.Value.HitEntity;

        if (!TryComp<ContainmentFieldGeneratorComponent?>(ent, out var otherFieldGeneratorComponent) ||
            otherFieldGeneratorComponent == component ||
            !TryComp<PhysicsComponent>(ent, out var collidableComponent) ||
            collidableComponent.BodyType != BodyType.Static ||
            gen1XForm.ParentUid != Transform(otherFieldGeneratorComponent.Owner).ParentUid)
        {
            return false;
        }

        var fields = GenerateFieldConnection(component, otherFieldGeneratorComponent);

        component.Connections[dir] = (otherFieldGeneratorComponent, fields);
        otherFieldGeneratorComponent.Connections[dir.GetOpposite()] = (component, fields);
        ChangeFieldVisualizer(otherFieldGeneratorComponent);

        if (!component.IsConnected)
        {
            component.IsConnected = true;
            ChangeOnLightVisualizer(component);
        }

        if (!otherFieldGeneratorComponent.IsConnected)
        {
            otherFieldGeneratorComponent.IsConnected = true;
            ChangeOnLightVisualizer(otherFieldGeneratorComponent);
        }

        ChangeFieldVisualizer(component);
        UpdateConnectionLights(component);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-connected"), component.Owner, Filter.Pvs(component.Owner));
        return true;
    }

    /// <summary>
    /// Spawns fields between two generators if the <see cref="TryGenerateFieldConnection"/> finds two generators to connect.
    /// </summary>
    /// <param name="firstGenComp">The source field generator</param>
    /// <param name="secondGenComp">The second generator that the source is connected to</param>
    /// <returns></returns>
    private List<EntityUid> GenerateFieldConnection(ContainmentFieldGeneratorComponent firstGenComp, ContainmentFieldGeneratorComponent secondGenComp)
    {
        var fieldList = new List<EntityUid>();
        var gen1Coords = Transform(firstGenComp.Owner).Coordinates;
        var gen2Coords = Transform(secondGenComp.Owner).Coordinates;

        var delta = (gen2Coords - gen1Coords).Position;
        var dirVec = delta.Normalized;
        var stopDist = delta.Length;
        var currentOffset = dirVec;
        while (currentOffset.Length < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            var newField = Spawn(firstGenComp.CreatedField, currentCoords);

            var fieldXForm = Transform(newField);
            fieldXForm.AttachParent(firstGenComp.Owner);
            if (dirVec.GetDir() == Direction.East || dirVec.GetDir() == Direction.West)
            {
                var angle = fieldXForm.LocalPosition.ToAngle();
                var rotateBy90 = angle.Degrees + 90;
                var rotatedAngle = Angle.FromDegrees(rotateBy90);

                fieldXForm.LocalRotation = rotatedAngle;
            }

            fieldList.Add(newField);
            currentOffset += dirVec;
        }
        return fieldList;
    }

    /// <summary>
    /// Creates a light component for the spawned fields.
    /// </summary>
    public void UpdateConnectionLights(ContainmentFieldGeneratorComponent component)
    {
        if (EntityManager.TryGetComponent<PointLightComponent>(component.Owner, out var pointLightComponent))
        {
            bool hasAnyConnection = component.Connections != null;
            pointLightComponent.Enabled = hasAnyConnection;
        }
    }

    /// <summary>
    /// Checks to see if this or the other gens connected to a new grid. If they did, remove connection.
    /// </summary>
    public void GridCheck(ContainmentFieldGeneratorComponent component)
    {
        var xFormQuery = GetEntityQuery<TransformComponent>();

        foreach (var (_, generators) in component.Connections)
        {
            var gen1ParentGrid = xFormQuery.GetComponent(component.Owner).ParentUid;
            var gent2ParentGrid = xFormQuery.GetComponent(generators.Item1.Owner).ParentUid;

            if (gen1ParentGrid != gent2ParentGrid)
                RemoveConnections(component);
        }
    }

    #endregion

    #region VisualizerHelpers
    /// <summary>
    /// Check if a fields power falls between certain ranges to update the field gen visual for power.
    /// </summary>
    /// <param name="power"></param>
    /// <param name="component"></param>
    private void ChangePowerVisualizer(int power, ContainmentFieldGeneratorComponent component)
    {
        if (!TryComp<AppearanceComponent>(component.Owner, out var appearance))
            return;

        if(component.PowerBuffer == 0)
            appearance.SetData(ContainmentFieldGeneratorVisuals.PowerLight, PowerLevelVisuals.NoPower);

        if (component.PowerBuffer > 0 && component.PowerBuffer < component.PowerMinimum)
            appearance.SetData(ContainmentFieldGeneratorVisuals.PowerLight, PowerLevelVisuals.LowPower);

        if (component.PowerBuffer >= component.PowerMinimum && component.PowerBuffer < 25)
        {
            appearance.SetData(ContainmentFieldGeneratorVisuals.PowerLight, PowerLevelVisuals.MediumPower);
        }

        if (component.PowerBuffer == 25)
        {
            appearance.SetData(ContainmentFieldGeneratorVisuals.PowerLight, PowerLevelVisuals.HighPower);
        }
    }

    /// <summary>
    /// Check if a field has any or no connections and if it's enabled to toggle the field level light
    /// </summary>
    /// <param name="component"></param>
    private void ChangeFieldVisualizer(ContainmentFieldGeneratorComponent component)
    {
        if (!TryComp<AppearanceComponent>(component.Owner, out var appearance))
            return;

        if (component.Connections.Count == 0 && !component.Enabled)
        {
            appearance.SetData(ContainmentFieldGeneratorVisuals.FieldLight, FieldLevelVisuals.NoLevel);
        }

        if (component.Connections.Count == 0 && component.Enabled)
        {
            appearance.SetData(ContainmentFieldGeneratorVisuals.FieldLight, FieldLevelVisuals.On);
        }

        if (component.Connections.Count == 1)
        {
            appearance.SetData(ContainmentFieldGeneratorVisuals.FieldLight, FieldLevelVisuals.OneField);
        }

        if (component.Connections.Count > 1)
        {
            appearance.SetData(ContainmentFieldGeneratorVisuals.FieldLight, FieldLevelVisuals.MultipleFields);
        }
    }

    private void ChangeOnLightVisualizer(ContainmentFieldGeneratorComponent component)
    {
        if (!TryComp<AppearanceComponent>(component.Owner, out var appearance))
            return;

        appearance.SetData(ContainmentFieldGeneratorVisuals.OnLight, component.IsConnected);
    }
    #endregion
}
