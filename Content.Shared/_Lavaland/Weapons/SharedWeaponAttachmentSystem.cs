using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.Components;
using Content.Shared.Light.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lavaland.Weapons;

public abstract partial class SharedWeaponAttachmentSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponAttachmentComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WeaponAttachmentComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WeaponAttachmentComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<WeaponAttachmentComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<WeaponAttachmentComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
    }

    private void OnMapInit(EntityUid uid, WeaponAttachmentComponent component, MapInitEvent args)
    {
        var itemSlots = EnsureComp<ItemSlotsComponent>(uid);
        var bayonetSlot = new ItemSlot
        {
            Whitelist = new EntityWhitelist { Components = ["AttachmentBayonet"] },
            Swap = false,
            EjectOnBreak = true,
            Name = Loc.GetString("attachment-bayonet-slot-name")
        };
        var lightSlot = new ItemSlot
        {
            Whitelist = new EntityWhitelist { Components = ["AttachmentFlashlight"] },
            Swap = false,
            EjectOnBreak = true,
            Name = Loc.GetString("attachment-light-slot-name"),
        };
        var scopeSlot = new ItemSlot
        {
            Whitelist = new EntityWhitelist { Components = ["AttachmentScope"] },
            Swap = false,
            EjectOnBreak = true,
            Name = Loc.GetString("attachment-scope-slot-name"),
        };
        _itemSlots.AddItemSlot(uid, WeaponAttachmentComponent.BayonetSlotId, bayonetSlot, itemSlots);
        _itemSlots.AddItemSlot(uid, WeaponAttachmentComponent.LightSlotId, lightSlot, itemSlots);
        _itemSlots.AddItemSlot(uid, WeaponAttachmentComponent.ScopeSlotId, scopeSlot, itemSlots);

        var lightContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, WeaponAttachmentComponent.LightSlotId);
        lightContainer.OccludesLight = false;
        _containerSystem.EnsureContainer<ContainerSlot>(uid, WeaponAttachmentComponent.ScopeSlotId);
    }

    private void OnShutdown(EntityUid uid, WeaponAttachmentComponent component, ComponentShutdown args)
    {
        RemoveToggleAction(component);
    }

    private void OnGetActions(EntityUid uid, WeaponAttachmentComponent component, GetItemActionsEvent args)
    {
        if (component.LightAttached && component.ToggleLightAction != null)
            args.AddAction(ref component.ToggleLightAction, component.LightActionPrototype);
    }

    private void CreateToggleAction(EntityUid uid, WeaponAttachmentComponent component)
    {
        if (component.ToggleLightAction != null)
            return;

        _actions.AddAction(uid, ref component.ToggleLightAction, component.LightActionPrototype);
    }

    private void RemoveToggleAction(WeaponAttachmentComponent component)
    {
        if (component.ToggleLightAction == null)
            return;

        _actions.RemoveAction(component.ToggleLightAction.Value);
        component.ToggleLightAction = null;
    }

    private void OnEntInsertedIntoContainer(EntityUid uid, WeaponAttachmentComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == WeaponAttachmentComponent.BayonetSlotId && HasComp<AttachmentBayonetComponent>(args.Entity))
            BayonetChanged(uid, true, component);
        else if (args.Container.ID == WeaponAttachmentComponent.LightSlotId && HasComp<AttachmentFlashlightComponent>(args.Entity))
            AttachLight(uid, args.Entity, component);
        else if (args.Container.ID == WeaponAttachmentComponent.ScopeSlotId && HasComp<AttachmentScopeComponent>(args.Entity))
            AttachScope(uid, args.Entity, component);
    }

    private void OnEntRemovedFromContainer(EntityUid uid, WeaponAttachmentComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == WeaponAttachmentComponent.BayonetSlotId && HasComp<AttachmentBayonetComponent>(args.Entity))
            BayonetChanged(uid, false, component);
        else if (args.Container.ID == WeaponAttachmentComponent.LightSlotId && HasComp<AttachmentFlashlightComponent>(args.Entity))
            RemoveLight(uid, component);
        else if (args.Container.ID == WeaponAttachmentComponent.ScopeSlotId && HasComp<AttachmentScopeComponent>(args.Entity))
            RemoveScope(uid, component);
    }

    private void BayonetChanged(EntityUid uid, bool attached, WeaponAttachmentComponent component)
    {
        if (component.BayonetAttached == attached || !TryComp<MeleeWeaponComponent>(uid, out var meleeComp))
            return;

        component.BayonetAttached = attached;

        if (attached)
        {
            meleeComp.Damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 12);
            AddSharp(uid);
        }
        else
        {
            meleeComp.Damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 5);
            RemSharp(uid);
        }

        Dirty(uid, component);
    }

    protected abstract void AddSharp(EntityUid uid);
    protected abstract void RemSharp(EntityUid uid);

    private void AttachLight(EntityUid uid, EntityUid light, WeaponAttachmentComponent component)
    {
        if (component.LightAttached)
            return;

        component.LightAttached = true;
        if (TryComp<HandheldLightComponent>(light, out var lightComp))
            component.LightOn = lightComp.Activated;

        CreateToggleAction(uid, component);

        if (TryComp<HandsComponent>(Transform(uid).ParentUid, out var hands))
        {
            var ev = new GetItemActionsEvent(_actionContainer, Transform(uid).ParentUid, uid);
            RaiseLocalEvent(uid, ev);
        }

        Dirty(uid, component);
    }

    private void RemoveLight(EntityUid uid, WeaponAttachmentComponent component)
    {
        if (!component.LightAttached)
            return;

        component.LightAttached = false;
        component.LightOn = false;
        RemoveToggleAction(component);
        Dirty(uid, component);
    }

    private void AttachScope(EntityUid uid, EntityUid scope, WeaponAttachmentComponent component)
    {
        if (component.ScopeAttached)
            return;

        component.ScopeAttached = true;
        AddScope(uid, scope);
        Dirty(uid, component);
    }

    private void RemoveScope(EntityUid uid, WeaponAttachmentComponent component)
    {
        if (!component.ScopeAttached)
            return;

        component.ScopeAttached = false;
        RemScope(uid);
        Dirty(uid, component);
    }

    protected abstract void AddScope(EntityUid uid, EntityUid scope);
    protected abstract void RemScope(EntityUid uid);
}
