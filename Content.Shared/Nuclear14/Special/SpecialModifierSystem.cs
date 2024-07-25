using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.DoAfter;
using Content.Shared.Nuclear14.Special.Components;

namespace Content.Shared.Nuclear14.Special
{
    public sealed class SpecialModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpecialComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SpecialComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, SpecialComponent component, ref ComponentGetState args)
        {
            args.State = new SpecialModifierComponentState
            {
                StrengthModifier = component.StrengthModifier,
                PerceptionModifier = component.PerceptionModifier,
                EnduranceModifier = component.EnduranceModifier,
                CharismaModifier = component.CharismaModifier,
                IntelligenceModifier = component.IntelligenceModifier,
                AgilityModifier = component.AgilityModifier,
                LuckModifier = component.LuckModifier,
            };
        }

        private void OnHandleState(EntityUid uid, SpecialComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not SpecialModifierComponentState state) return;
            component.StrengthModifier = state.StrengthModifier;
            component.PerceptionModifier = state.PerceptionModifier;
            component.EnduranceModifier = state.EnduranceModifier;
            component.CharismaModifier = state.CharismaModifier;
            component.IntelligenceModifier = state.IntelligenceModifier;
            component.AgilityModifier = state.AgilityModifier;
            component.LuckModifier = state.LuckModifier;
        }

        public void RefreshClothingSpecialModifiers(EntityUid uid, SpecialComponent? special = null)
        {
            if (!Resolve(uid, ref special, false))
                return;

            if (_timing.ApplyingState)
                return;

            var ev = new RefreshSpecialModifiersEvent();
            RaiseLocalEvent(uid, ev);

            if (ev.StrengthModifier == special.StrengthModifier &&
                ev.PerceptionModifier == special.PerceptionModifier &&
                ev.EnduranceModifier == special.EnduranceModifier &&
                ev.CharismaModifier == special.CharismaModifier &&
                ev.IntelligenceModifier == special.IntelligenceModifier &&
                ev.AgilityModifier == special.AgilityModifier &&
                ev.LuckModifier == special.LuckModifier
            )  return;

            special.StrengthModifier = ev.StrengthModifier;
            special.PerceptionModifier = ev.PerceptionModifier;
            special.EnduranceModifier = ev.EnduranceModifier;
            special.CharismaModifier = ev.CharismaModifier;
            special.IntelligenceModifier = ev.IntelligenceModifier;
            special.AgilityModifier = ev.AgilityModifier;
            special.LuckModifier = ev.LuckModifier;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, 0, new RefreshSpecialModifiersDoAfterEvent(), uid, uid)
            {
                BreakOnDamage = false,
                NeedHand = false,
                RequireCanInteract = false,
            };
            if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
                return;

            Dirty(special);
        }


        [Serializable, NetSerializable]
        private sealed class SpecialModifierComponentState : ComponentState
        {
            public int StrengthModifier;
            public int PerceptionModifier;
            public int EnduranceModifier;
            public int CharismaModifier;
            public int IntelligenceModifier;
            public int AgilityModifier;
            public int LuckModifier;

        }
    }

    /// <summary>
    ///     Raised on an entity to determine its special modificators. Any system that wishes to change special modificators
    ///     should hook into this event and set it then. If you want this event to be raised,
    ///     call <see cref="SpecialModifierSystem.RefreshSpecialModifiersEvent"/>.
    /// </summary>
    public sealed class RefreshSpecialModifiersEvent : EntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;
        public int StrengthModifier { get; private set; } = 0;
        public int PerceptionModifier { get; private set; } = 0;
        public int EnduranceModifier { get; private set; } = 0;
        public int CharismaModifier { get; private set; } = 0;
        public int IntelligenceModifier { get; private set; } = 0;
        public int AgilityModifier { get; private set; } = 0;
        public int LuckModifier { get; private set; } = 0;

        public void ModifySpecial(
            int strengthModifier,
            int perceptionModifier,
            int enduranceModifier,
            int charismaModifier,
            int intelligenceModifier,
            int agilityModifier,
            int luckModifier
        )
        {
            StrengthModifier += strengthModifier;
            PerceptionModifier += perceptionModifier;
            EnduranceModifier += enduranceModifier;
            CharismaModifier += charismaModifier;
            IntelligenceModifier += intelligenceModifier;
            AgilityModifier += agilityModifier;
            LuckModifier += luckModifier;
        }

        // used to modify stats by chems
        public void ModifyStrength(int strengthModifier)
        {
            StrengthModifier += strengthModifier;
        }
        public void ModifyPerception(int perceptionModifier)
        {
            PerceptionModifier += perceptionModifier;
        }
        public void ModifyEndurance(int enduranceModifier)
        {
            EnduranceModifier += enduranceModifier;
        }
        public void ModifyCharisma(int charismaModifier)
        {
            CharismaModifier += charismaModifier;
        }
        public void ModifyIntelligence(int intelligenceModifier)
        {
            IntelligenceModifier += intelligenceModifier;
        }
        public void ModifyAgility(int agilityModifier)
        {
            AgilityModifier += agilityModifier;
        }
        public void ModifyLuck(int luckModifier)
        {
            LuckModifier += luckModifier;
        }
    }
    [Serializable, NetSerializable]
    public sealed partial class RefreshSpecialModifiersDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
