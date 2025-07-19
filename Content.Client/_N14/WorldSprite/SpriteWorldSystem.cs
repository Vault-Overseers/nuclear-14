using Content.Shared.Throwing;
using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Client._N14.WorldSprite;

public sealed class SpriteWorldSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteWorldComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpriteWorldComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<SpriteWorldComponent, ThrownEvent>(OnThrown);
    }

    private void OnInit(EntityUid uid, SpriteWorldComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        component.DefaultState = sprite.LayerGetState(0);
        UpdateSprite(uid, component, sprite, Transform(uid).ParentUid);
    }

    private void OnParentChanged(EntityUid uid, SpriteWorldComponent component, ref EntParentChangedMessage args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        UpdateSprite(uid, component, sprite, args.NewParent);
    }

    private void OnThrown(EntityUid uid, SpriteWorldComponent component, ref ThrownEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        UpdateSprite(uid, component, sprite, Transform(uid).ParentUid);
    }

    private void UpdateSprite(EntityUid uid, SpriteWorldComponent component, SpriteComponent sprite, EntityUid parent)
    {
        var inWorld = HasComp<MapComponent>(parent) || HasComp<MapGridComponent>(parent);
        var state = inWorld && !string.IsNullOrEmpty(component.WorldState)
            ? component.WorldState
            : component.DefaultState;

        if (state != null)
            sprite.LayerSetState(0, state);
    }
}
