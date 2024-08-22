using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.DoAfter;
using Content.Shared.Nuclear14.Special.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Nuclear14.Special
{
    public sealed class SpecialModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpecialComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SpecialComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<SpecialComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SpecialComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);

        }

        private void OnGetExamineVerbs(EntityUid uid, SpecialComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
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

            if  (strength != 0 ||
                perception != 0 ||
            endurance != 0 ||
            charisma != 0 ||
            intelligence != 0 ||
            agility != 0 ||
            luck != 0
            )
            _examine.AddDetailedExamineVerb(args,
                component,
                msg,
                Loc.GetString("special-examinable-component-examine-text"),
                "/Textures/Interface/examine-star.png",
                Loc.GetString("special-examinable-verb-message")
            );
        }

        private void OnExamined(EntityUid uid, SpecialComponent component, ExaminedEvent args)
        {
            var charisma = component.TotalCharisma;
            var identity = Identity.Entity(uid, EntityManager);

            switch(charisma){
                case 1 or 2:
                    args.PushText(Loc.GetString("special-appearance-component-examine-charisma-very-low", ("user", identity)));
                    break;
                case 3 or 4:
                    args.PushText(Loc.GetString("special-appearance-component-examine-charisma-very-low", ("user", identity)));
                    break;
                case 5 or 6:
                    args.PushText(Loc.GetString("special-appearance-component-examine-charisma-medium", ("user", identity)));
                    break;
                case 7 or 8:
                    args.PushText(Loc.GetString("special-appearance-component-examine-charisma-high", ("user", identity)));
                    break;
                case 9 or 10:
                    args.PushText(Loc.GetString("special-appearance-component-examine-charisma-very-high", ("user", identity)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
