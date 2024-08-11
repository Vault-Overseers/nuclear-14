using Content.Server.GameTicking;
using Content.Server.Nuclear14.Special.Speech.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using Content.Shared.Nuclear14.Special.Components;
using Content.Shared.Nuclear14.Special;
using Content.Shared.Nuclear14.Special.Components;
using Content.Server.NPC.Systems;
using Content.Server.Roles;

namespace Content.Server.Nuclear.Special.EntitySystems;

public sealed class SpecialSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecialComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<SpecialComponent, RefreshSpecialModifiersDoAfterEvent>(OnSpecialModifiersChanged);

        SubscribeLocalEvent<SpecialComponent, GetBriefingEvent>(OnGetBriefing);
    }

        private void OnGetBriefing(EntityUid uid, SpecialComponent component, ref GetBriefingEvent args)
        {
            if (!TryComp<MindComponent>(uid, out var mind) || mind.OwnedEntity == null)
                return;

            var strength = component.TotalStrength;
            var perception = component.TotalPerception;
            var endurance = component.TotalEndurance;
            var charisma = component.TotalCharisma;
            var intelligence = component.TotalIntelligence;
            var agility = component.TotalAgility;
            var luck = component.TotalLuck;

            var msg = new FormattedMessage();
            if (strength != 0)
            {
                msg.AddMarkup(Loc.GetString("special-appearance-component-examine-strength", ("value", strength)));
                msg.PushNewline();
            }
            if (perception != 0)
            {
                msg.AddMarkup(Loc.GetString("special-appearance-component-examine-perception", ("value", perception)));
                msg.PushNewline();
            }
            if (endurance != 0)
            {
                msg.AddMarkup(Loc.GetString("special-appearance-component-examine-endurance", ("value", endurance)));
                msg.PushNewline();
            }
            if (charisma != 0)
            {
                msg.AddMarkup(Loc.GetString("special-appearance-component-examine-charisma", ("value", charisma)));
                msg.PushNewline();
            }
            if (intelligence != 0)
            {
                msg.AddMarkup(Loc.GetString("special-appearance-component-examine-intelligence", ("value", intelligence)));
                msg.PushNewline();
            }
            if (agility != 0)
            {
                msg.AddMarkup(Loc.GetString("special-appearance-component-examine-agility", ("value", agility)));
                msg.PushNewline();
            }
            if (luck != 0)
            {
                msg.AddMarkup(Loc.GetString("special-appearance-component-examine-luck", ("value", luck)));
                msg.PushNewline();
            }

            args.Append(msg.ToString());
        }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(EntityUid uid, SpecialComponent component, PlayerSpawnCompleteEvent args)
    {
        if (!EntityManager.TryGetComponent<SpecialComponent>(uid, out var special))
        {
            return;
        }

        var sum = 0;
        foreach (var item in args.Profile.SpecialPriorities)
        {
            if (!_prototypeManager.TryIndex<SpecialPrototype>(item.Key, out var specialPrototype))
            {
                Logger.Warning($"No special prototype found with ID {item.Key}!");
                return;
            }
            sum += (int) item.Value;
        }

        foreach (var item in args.Profile.SpecialPriorities)
        {
            if (!_prototypeManager.TryIndex<SpecialPrototype>(item.Key, out var specialPrototype))
            {
                Logger.Warning($"No special prototype found with ID {item.Key}!");
                return;
            }
            if(sum > 40)
                setSpecial(special, specialPrototype, SpecialPriority.Five);
            else
                setSpecial(special, specialPrototype, item.Value);
        }

        if (special.TotalIntelligence < 3)
        {
            EntityManager.AddComponent<LowIntelligenceAccentComponent>(uid);
        }
        else
        {
            EntityManager.RemoveComponent<LowIntelligenceAccentComponent>(uid);
        }

        if(!TryComp<MindContainerComponent>(uid, out var mindContainer) || mindContainer.Mind == null)
            return;

        var mindId = mindContainer.Mind.Value;
        var mind = Comp<MindComponent>(mindContainer.Mind.Value);

        mind.ClearSpecial();
        mind.AddSpecial(Loc.GetString("special-component-examine-character-strength", ("base", special.BaseStrength), ("modifier", special.StrengthModifier), ("total", special.TotalStrength)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-perception", ("base", special.BasePerception), ("modifier", special.PerceptionModifier), ("total", special.TotalPerception)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-endurance", ("base", special.BaseEndurance), ("modifier", special.EnduranceModifier), ("total", special.TotalEndurance)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-charisma", ("base", special.BaseCharisma), ("modifier", special.CharismaModifier), ("total", special.TotalCharisma)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-intelligence", ("base", special.BaseIntelligence), ("modifier", special.IntelligenceModifier), ("total", special.TotalIntelligence)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-agility", ("base", special.BaseAgility), ("modifier", special.AgilityModifier), ("total", special.TotalAgility)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-luck", ("base", special.BaseLuck), ("modifier", special.LuckModifier), ("total", special.TotalLuck)));

    }

    private void setSpecial(SpecialComponent component,
        SpecialPrototype prototype,
        SpecialPriority priority)
    {
        switch(prototype.ID)
        {
            case "Strength":
                component.BaseStrength = (int) priority;
                return;
            case "Perception":
                component.BasePerception = (int) priority;
                return;
            case "Endurance":
                component.BaseEndurance = (int) priority;
                return;
            case "Charisma":
                component.BaseCharisma = (int) priority;
                return;
            case "Intelligence":
                component.BaseIntelligence = (int) priority;
                return;
            case "Agility":
                component.BaseAgility = (int) priority;
                return;
            case "Luck":
                component.BaseLuck = (int) priority;
                return;
            default:
                return;
        }
    }

    private void OnSpecialModifiersChanged(EntityUid uid, SpecialComponent component, RefreshSpecialModifiersDoAfterEvent args)
    {
        if (component.TotalIntelligence < 3)
        {
            EnsureComp<LowIntelligenceAccentComponent>(uid);
        }
        else
        {
            EntityManager.RemoveComponent<LowIntelligenceAccentComponent>(uid);
        }

        if(!TryComp<MindContainerComponent>(uid, out var mindContainer) || mindContainer.Mind == null)
            return;

        var mindId = mindContainer.Mind.Value;
        var mind = Comp<MindComponent>(mindContainer.Mind.Value);

        mind.ClearSpecial();
        mind.AddSpecial(Loc.GetString("special-component-examine-character-strength", ("base", component.BaseStrength), ("modifier", component.StrengthModifier), ("total", component.TotalStrength)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-perception", ("base", component.BasePerception), ("modifier", component.PerceptionModifier), ("total", component.TotalPerception)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-endurance", ("base", component.BaseEndurance), ("modifier", component.EnduranceModifier), ("total", component.TotalEndurance)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-charisma", ("base", component.BaseCharisma), ("modifier", component.CharismaModifier), ("total", component.TotalCharisma)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-intelligence", ("base", component.BaseIntelligence), ("modifier", component.IntelligenceModifier), ("total", component.TotalIntelligence)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-agility", ("base", component.BaseAgility), ("modifier", component.AgilityModifier), ("total", component.TotalAgility)));
        mind.AddSpecial(Loc.GetString("special-component-examine-character-luck", ("base", component.BaseLuck), ("modifier", component.LuckModifier), ("total", component.TotalLuck)));


    }
}
