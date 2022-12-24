using Content.Client.Actions;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Client.Decals;

// This is shit and basically a half-rewrite of PlacementManager
// TODO refactor placementmanager so this isnt shit anymore
public sealed class DecalPlacementSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    private string? _decalId;
    private Color _decalColor = Color.White;
    private Angle _decalAngle = Angle.Zero;
    private bool _snap;
    private int _zIndex;
    private bool _cleanable;

    private bool _active;
    private bool _placing;
    private bool _erasing;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder.Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
                if (!_active || _placing || _decalId == null)
                    return false;

                _placing = true;

                if (_snap)
                {
                    var newPos = new Vector2(
                        (float) (MathF.Round(coords.X - 0.5f, MidpointRounding.AwayFromZero) + 0.5),
                        (float) (MathF.Round(coords.Y - 0.5f, MidpointRounding.AwayFromZero) + 0.5)
                    );
                    coords = coords.WithPosition(newPos);
                }

                coords = coords.Offset(new Vector2(-0.5f, -0.5f));

                if (!coords.IsValid(EntityManager))
                    return false;

                var decal = new Decal(coords.Position, _decalId, _decalColor, _decalAngle, _zIndex, _cleanable);
                RaiseNetworkEvent(new RequestDecalPlacementEvent(decal, coords));

                return true;
            },
            (session, coords, uid) =>
            {
                if (!_active)
                    return false;

                _placing = false;
                return true;
            }, true))
            .Bind(EngineKeyFunctions.EditorCancelPlace, new PointerStateInputCmdHandler(
            (session, coords, uid) =>
            {
                if (!_active || _erasing)
                    return false;

                _erasing = true;

                RaiseNetworkEvent(new RequestDecalRemovalEvent(coords));

                return true;
            }, (session, coords, uid) =>
            {
                if (!_active)
                    return false;
                _erasing = false;

                return true;
            }, true)).Register<DecalPlacementSystem>();

        SubscribeLocalEvent<FillActionSlotEvent>(OnFillSlot);
        SubscribeLocalEvent<PlaceDecalActionEvent>(OnPlaceDecalAction);
    }

    private void OnPlaceDecalAction(PlaceDecalActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_mapMan.TryFindGridAt(args.Target, out var grid))
            return;

        args.Handled = true;

        var coords =  EntityCoordinates.FromMap(grid.GridEntityId, args.Target, EntityManager);

        if (args.Snap)
        {
            var newPos = new Vector2(
                (float) (MathF.Round(coords.X - 0.5f, MidpointRounding.AwayFromZero) + 0.5),
                (float) (MathF.Round(coords.Y - 0.5f, MidpointRounding.AwayFromZero) + 0.5)
            );
            coords = coords.WithPosition(newPos);
        }

        coords = coords.Offset(new Vector2(-0.5f, -0.5f));

        var decal = new Decal(coords.Position, args.DecalId, args.Color, Angle.FromDegrees(args.Rotation), args.ZIndex, args.Cleanable);
        RaiseNetworkEvent(new RequestDecalPlacementEvent(decal, coords));
    }

    private void OnFillSlot(FillActionSlotEvent ev)
    {
        if (!_active || _placing)
            return;

        if (ev.Action != null)
            return;

        if (_decalId == null || !_protoMan.TryIndex<DecalPrototype>(_decalId, out var decalProto))
            return;

        var actionEvent = new PlaceDecalActionEvent()
        {
            DecalId = _decalId,
            Color = _decalColor,
            Rotation = _decalAngle.Degrees,
            Snap = _snap,
            ZIndex = _zIndex,
            Cleanable = _cleanable,
        };

        ev.Action = new WorldTargetAction()
        {
            DisplayName = $"{_decalId} ({_decalColor.ToHex()}, {(int) _decalAngle.Degrees})", // non-unique actions may be considered duplicates when saving/loading.
            Icon = decalProto.Sprite,
            Repeat = true,
            CheckCanAccess = false,
            CheckCanInteract = false,
            Range = -1,
            Event = actionEvent,
            IconColor = _decalColor,
        };
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<DecalPlacementSystem>();
    }

    public void UpdateDecalInfo(string id, Color color, float rotation, bool snap, int zIndex, bool cleanable)
    {
        _decalId = id;
        _decalColor = color;
        _decalAngle = Angle.FromDegrees(rotation);
        _snap = snap;
        _zIndex = zIndex;
        _cleanable = cleanable;
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (_active)
            _inputManager.Contexts.SetActiveContext("editor");
        else
            _inputSystem.SetEntityContextActive();
    }
}

public sealed class PlaceDecalActionEvent : WorldTargetActionEvent
{
    [DataField("decalId", customTypeSerializer:typeof(PrototypeIdSerializer<DecalPrototype>))]
    public string DecalId = string.Empty;

    [DataField("color")]
    public Color Color;

    [DataField("rotation")]
    public double Rotation;

    [DataField("snap")]
    public bool Snap;

    [DataField("zIndex")]
    public int ZIndex;

    [DataField("cleanable")]
    public bool Cleanable;
}
