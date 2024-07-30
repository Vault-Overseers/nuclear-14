using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Content.Shared.Nuclear14.Special;
using Content.Shared.Nuclear14.Special.Components;

namespace Content.Shared.Nuclear14.Special;

public sealed partial class ClothingSpecialModifierSystem : EntitySystem
{
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpecialModifierComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingSpecialModifierComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingSpecialModifierComponent, InventoryRelayedEvent<RefreshSpecialModifiersEvent>>(OnRefreshModifiers);
        SubscribeLocalEvent<ClothingSpecialModifierComponent, GetVerbsEvent<ExamineVerb>>(OnClothingVerbExamine);
    }

    // Public API

    public void SetClothingSpecialModifierEnabled(EntityUid uid, bool enabled, ClothingSpecialModifierComponent? component = null)
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
                _specialModifiers.RefreshClothingSpecialModifiers(container.Owner);
            }
        }
    }

    // Event handlers

    private void OnGetState(EntityUid uid, ClothingSpecialModifierComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingSpecialModifierComponentState(
            component.StrengthModifier,
            component.PerceptionModifier,
            component.EnduranceModifier,
            component.CharismaModifier,
            component.IntelligenceModifier,
            component.AgilityModifier,
            component.LuckModifier,
            component.Enabled);
    }

    private void OnHandleState(EntityUid uid, ClothingSpecialModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ClothingSpecialModifierComponentState state)
            return;

        var diff = component.Enabled != state.Enabled ||
                !MathHelper.CloseTo(component.StrengthModifier, state.StrengthModifier) ||
                !MathHelper.CloseTo(component.PerceptionModifier, state.PerceptionModifier) ||
                !MathHelper.CloseTo(component.EnduranceModifier, state.EnduranceModifier) ||
                !MathHelper.CloseTo(component.CharismaModifier, state.CharismaModifier) ||
                !MathHelper.CloseTo(component.IntelligenceModifier, state.IntelligenceModifier) ||
                !MathHelper.CloseTo(component.AgilityModifier, state.AgilityModifier) ||
                !MathHelper.CloseTo(component.LuckModifier, state.LuckModifier);

        component.StrengthModifier = state.StrengthModifier;
        component.PerceptionModifier = state.PerceptionModifier;
        component.EnduranceModifier = state.EnduranceModifier;
        component.CharismaModifier = state.CharismaModifier;
        component.IntelligenceModifier = state.IntelligenceModifier;
        component.AgilityModifier = state.AgilityModifier;
        component.LuckModifier = state.LuckModifier;
        component.Enabled = state.Enabled;

        // Avoid raising the event for the container if nothing changed.
        // We'll still set the values in case they're slightly different but within tolerance.
        if (diff && _container.TryGetContainingContainer(uid, out var container))
        {
            _specialModifiers.RefreshClothingSpecialModifiers(container.Owner);
        }
    }

    private void OnRefreshModifiers(EntityUid uid, ClothingSpecialModifierComponent component, InventoryRelayedEvent<RefreshSpecialModifiersEvent> args)
    {
        if (!component.Enabled)
            return;

        args.Args.ModifySpecial(component.StrengthModifier,
            component.PerceptionModifier,
            component.EnduranceModifier,
            component.CharismaModifier,
            component.IntelligenceModifier,
            component.AgilityModifier,
            component.LuckModifier
        );
    }

    private void OnClothingVerbExamine(EntityUid uid, ClothingSpecialModifierComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var strengthModifier = component.StrengthModifier;
        var perceptionModifier = component.PerceptionModifier;
        var enduranceModifier = component.EnduranceModifier;
        var charismaModifier = component.CharismaModifier;
        var intelligenceModifier = component.IntelligenceModifier;
        var agilityModifier = component.AgilityModifier;
        var luckModifier = component.LuckModifier;

        var msg = new FormattedMessage();

        if (strengthModifier != 0){
        if  (strengthModifier > 0){
            msg.AddMarkup(Loc.GetString("clothing-strength-increase-equal-examine", ("strength", strengthModifier)));
            }
        else if (strengthModifier < 0)
            msg.AddMarkup(Loc.GetString("clothing-strength-decrease-equal-examine", ("strength", strengthModifier)));
        msg.PushNewline();
        }

        if (perceptionModifier != 0){
        if  (perceptionModifier > 0)
            msg.AddMarkup(Loc.GetString("clothing-perception-increase-equal-examine", ("perception", perceptionModifier)));
        else if (perceptionModifier < 0)
            msg.AddMarkup(Loc.GetString("clothing-perception-decrease-equal-examine", ("perception", perceptionModifier)));
        msg.PushNewline();
        }

        if (enduranceModifier != 0){
        if  (enduranceModifier > 0)
            msg.AddMarkup(Loc.GetString("clothing-endurance-increase-equal-examine", ("endurance", enduranceModifier)));
        else if (enduranceModifier < 0)
            msg.AddMarkup(Loc.GetString("clothing-endurance-decrease-equal-examine", ("endurance", enduranceModifier)));
        msg.PushNewline();
        }

        if (charismaModifier != 0){
        if  (charismaModifier > 0)
            msg.AddMarkup(Loc.GetString("clothing-charisma-increase-equal-examine", ("charisma", charismaModifier)));
        else if (charismaModifier < 0)
            msg.AddMarkup(Loc.GetString("clothing-charisma-decrease-equal-examine", ("charisma", charismaModifier)));
        msg.PushNewline();
        }

        if (intelligenceModifier != 0){
        if  (intelligenceModifier > 0)
            msg.AddMarkup(Loc.GetString("clothing-intelligence-increase-equal-examine", ("intelligence", intelligenceModifier)));
        else if (intelligenceModifier < 0)
            msg.AddMarkup(Loc.GetString("clothing-intelligence-decrease-equal-examine", ("intelligence", intelligenceModifier)));
        msg.PushNewline();
        }

        if (agilityModifier != 0){
        if  (agilityModifier > 0)
            msg.AddMarkup(Loc.GetString("clothing-agility-increase-equal-examine", ("agility", agilityModifier)));
        else if (agilityModifier < 0)
            msg.AddMarkup(Loc.GetString("clothing-agility-decrease-equal-examine", ("agility", agilityModifier)));
        msg.PushNewline();
        }

        if (luckModifier != 0){
        if  (luckModifier > 0)
            msg.AddMarkup(Loc.GetString("clothing-luck-increase-equal-examine", ("luck", luckModifier)));
        else if (luckModifier < 0)
            msg.AddMarkup(Loc.GetString("clothing-luck-decrease-equal-examine", ("luck", luckModifier)));
        msg.PushNewline();
        }

        if  (strengthModifier != 0 ||
        perceptionModifier != 0 ||
        enduranceModifier != 0 ||
        enduranceModifier != 0 ||
        charismaModifier != 0 ||
        intelligenceModifier != 0 ||
        agilityModifier != 0 ||
        luckModifier != 0
        )
        _examine.AddDetailedExamineVerb(args,
            component,
            msg,
            Loc.GetString("clothing-special-examinable-verb-text"),
            "/Textures/Interface/examine-star.png",
            Loc.GetString("clothing-special-examinable-verb-message")
        );
    }
}
