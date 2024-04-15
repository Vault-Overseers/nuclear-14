using Content.Server.GameTicking;
using Content.Server.Nuclear14.Special.Speech.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Nuclear14.Special.Components;
using Content.Shared.Nuclear14.Special;
using Content.Shared.Nuclear14.Special.Components;
using Content.Server.NPC.Systems;

namespace Content.Server.Nuclear.Special.EntitySystems;

public sealed class SpecialSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecialComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        
        SubscribeLocalEvent<SpecialComponent, RefreshSpecialModifiersDoAfterEvent>(OnSpecialModifiersChanged);

    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(EntityUid uid, SpecialComponent component, PlayerSpawnCompleteEvent args)
    {
        if (!EntityManager.TryGetComponent<SpecialComponent>(uid, out var special))
        {
            return;
        }

        foreach (var item in args.Profile.SpecialPriorities)
        {
            if (!_prototypeManager.TryIndex<SpecialPrototype>(item.Key, out var specialPrototype))
            {
                Logger.Warning($"No special prototype found with ID {item.Key}!");
                return;
            }
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

    }
}
