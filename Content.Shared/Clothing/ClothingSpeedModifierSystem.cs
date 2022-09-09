﻿using Content.Shared.Examine;
using Content.Shared.Movement.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;

public sealed class ClothingSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, GetVerbsEvent<ExamineVerb>>(OnClothingVerbExamine);
    }

    // Public API

    public void SetClothingSpeedModifierEnabled(EntityUid uid, bool enabled, ClothingSpeedModifierComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.Enabled != enabled)
        {
            component.Enabled = enabled;
            Dirty(component);

            // inventory system will automatically hook into the event raised by this and update accordingly
            if (_container.TryGetContainingContainer(uid, out var container))
            {
                _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
            }
        }
    }

    // Event handlers

    private void OnGetState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingSpeedModifierComponentState(component.WalkModifier, component.SprintModifier, component.Enabled);
    }

    private void OnHandleState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is ClothingSpeedModifierComponentState state)
        {
            component.WalkModifier = state.WalkModifier;
            component.SprintModifier = state.SprintModifier;
            component.Enabled = state.Enabled;

            if (_container.TryGetContainingContainer(uid, out var container))
            {
                _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
            }
        }
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ClothingSpeedModifierComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.Enabled)
            return;

        args.ModifySpeed(component.WalkModifier, component.SprintModifier);
    }

    private void OnClothingVerbExamine(EntityUid uid, ClothingSpeedModifierComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var walkModifierPercentage = MathF.Round((1.0f - component.WalkModifier) * 100f, 1);
        var sprintModifierPercentage = MathF.Round((1.0f - component.SprintModifier) * 100f, 1);

        if (walkModifierPercentage == 0.0f && sprintModifierPercentage == 0.0f)
            return;

        var msg = new FormattedMessage();

        if (walkModifierPercentage == sprintModifierPercentage)
        {
            if (walkModifierPercentage < 0.0f)
                msg.AddMarkup(Loc.GetString("clothing-speed-increase-equal-examine", ("walkSpeed", MathF.Abs(walkModifierPercentage)), ("runSpeed", MathF.Abs(sprintModifierPercentage))));
            else
                msg.AddMarkup(Loc.GetString("clothing-speed-decrease-equal-examine", ("walkSpeed", walkModifierPercentage), ("runSpeed", sprintModifierPercentage)));
        }
        else
        {
            if (sprintModifierPercentage < 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-increase-run-examine", ("runSpeed", MathF.Abs(sprintModifierPercentage))));
            }
            else if (sprintModifierPercentage > 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-decrease-run-examine", ("runSpeed", sprintModifierPercentage)));
            }
            if (walkModifierPercentage != 0.0f && sprintModifierPercentage != 0.0f)
            {
                msg.PushNewline();
            }
            if (walkModifierPercentage < 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-increase-walk-examine", ("walkSpeed", MathF.Abs(walkModifierPercentage))));
            }
            else if (walkModifierPercentage > 0.0f)
            {
                msg.AddMarkup(Loc.GetString("clothing-speed-decrease-walk-examine", ("walkSpeed", walkModifierPercentage)));
            }
        }

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                _examine.SendExamineTooltip(args.User, uid, msg, false, false);
            },
            Text = Loc.GetString("clothing-speed-examinable-verb-text"),
            Message = Loc.GetString("clothing-speed-examinable-verb-message"),
            Category = VerbCategory.Examine,
            IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png"
        };

        args.Verbs.Add(verb);
    }
}
