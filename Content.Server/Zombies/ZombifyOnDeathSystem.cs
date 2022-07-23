using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Server.Disease.Components;
using Content.Server.Body.Components;
using Content.Server.Atmos.Components;
using Content.Server.Nutrition.Components;
using Robust.Shared.Player;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Body.Systems;
using Content.Server.CombatMode;
using Content.Server.Inventory;
using Content.Server.Mind.Components;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Components;
using Content.Server.Mind.Commands;
using Content.Server.Temperature.Components;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.Containers;
using Content.Shared.Movement.Components;
using Content.Shared.MobState;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Server.Traitor;
using Content.Shared.Zombies;
using Content.Server.Atmos.Miasma;
using Content.Server.IdentityManagement;

namespace Content.Server.Zombies
{
    /// <summary>
    ///     Handles zombie propagation and inherent zombie traits
    /// </summary>
    /// <remarks>
    ///     Don't Shitcode Open Inside
    /// </remarks>
    public sealed class ZombifyOnDeathSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly ServerInventorySystem _serverInventory = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHuApp = default!;
        [Dependency] private readonly IdentitySystem _identity = default!;
        [Dependency] private readonly IChatManager _chatMan = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombifyOnDeathComponent, MobStateChangedEvent>(OnDamageChanged);
        }

        /// <summary>
        /// Handles an entity turning into a zombie when they die or go into crit
        /// </summary>
        private void OnDamageChanged(EntityUid uid, ZombifyOnDeathComponent component, MobStateChangedEvent args)
        {
            if (args.CurrentMobState.IsDead() ||
                args.CurrentMobState.IsCritical())
            {
                ZombifyEntity(uid);
            }
        }

        /// <summary>
        ///     This is the general purpose function to call if you want to zombify an entity.
        ///     It handles both humanoid and nonhumanoid transformation and everything should be called through it.
        /// </summary>
        /// <param name="target">the entity being zombified</param>
        /// <remarks>
        ///     ALRIGHT BIG BOY. YOU'VE COME TO THE LAYER OF THE BEAST. THIS IS YOUR WARNING.
        ///     This function is the god function for zombie stuff, and it is cursed. I have
        ///     attempted to label everything thouroughly for your sanity. I have attempted to
        ///     rewrite this, but this is how it shall lie eternal. Turn back now.
        ///     -emo
        /// </remarks>
        public void ZombifyEntity(EntityUid target)
        {
            //Don't zombfiy zombies
            if (HasComp<ZombieComponent>(target))
                return;

            //you're a real zombie now, son.
            var zombiecomp = AddComp<ZombieComponent>(target);

            ///we need to basically remove all of these because zombies shouldn't
            ///get diseases, breath, be thirst, be hungry, or die in space
            RemComp<DiseaseCarrierComponent>(target);
            RemComp<RespiratorComponent>(target);
            RemComp<BarotraumaComponent>(target);
            RemComp<HungerComponent>(target);
            RemComp<ThirstComponent>(target);

            //funny voice
            EnsureComp<ReplacementAccentComponent>(target).Accent = "zombie";
            EnsureComp<RottingComponent>(target);

            ///This is needed for stupid entities that fuck up combat mode component
            ///in an attempt to make an entity not attack. This is the easiest way to do it.
            RemComp<CombatModeComponent>(target);
            AddComp<CombatModeComponent>(target);

            ///This is the actual damage of the zombie. We assign the visual appearance
            ///and range here because of stuff we'll find out later
            var melee = EnsureComp<MeleeWeaponComponent>(target);
            melee.Arc = zombiecomp.AttackArc;
            melee.ClickArc = zombiecomp.AttackArc;
            melee.Range = 0.75f;

            //We have specific stuff for humanoid zombies because they matter more
            if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
            {
                //this bs is done because you can't directly update humanoid appearances
                var appearance = huApComp.Appearance;
                appearance = appearance.WithSkinColor(zombiecomp.SkinColor).WithEyeColor(zombiecomp.EyeColor);
                _sharedHuApp.UpdateAppearance(target, appearance, huApComp);
                _sharedHuApp.ForceAppearanceUpdate(target, huApComp);

                ///This is done here because non-humanoids shouldn't get baller damage
                ///lord forgive me for the hardcoded damage
                DamageSpecifier dspec = new();
                dspec.DamageDict.Add("Slash", 13);
                dspec.DamageDict.Add("Piercing", 7);
                melee.Damage = dspec;
            }

            //The zombie gets the assigned damage weaknesses and strengths
            _damageable.SetDamageModifierSetId(target, "Zombie");

            ///This makes it so the zombie doesn't take bloodloss damage.
            ///NOTE: they are supposed to bleed, just not take damage
            _bloodstream.SetBloodLossThreshold(target, 0f);

            //This is specifically here to combat insuls, because frying zombies on grilles is funny as shit.
            _serverInventory.TryUnequip(target, "gloves", true, true);

            //popup
            _popupSystem.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, Filter.Pvs(target));

            //Make it sentient if it's an animal or something
            if (!HasComp<InputMoverComponent>(target)) //this component is cursed and fucks shit up
                MakeSentientCommand.MakeSentient(target, EntityManager);

            //Make the zombie not die in the cold. Good for space zombies
            if (TryComp<TemperatureComponent>(target, out var tempComp))
                tempComp.ColdDamage.ClampMax(0);

            //Heals the zombie from all the damage it took while human
            if (TryComp<DamageableComponent>(target, out var damageablecomp))
                _damageable.SetAllDamage(damageablecomp, 0);

            //gives it the funny "Zombie ___" name.
            if (TryComp<MetaDataComponent>(target, out var meta))
                meta.EntityName = Loc.GetString("zombie-name-prefix", ("target", meta.EntityName));

            _identity.QueueIdentityUpdate(target);

            //He's gotta have a mind
            var mindcomp = EnsureComp<MindComponent>(target);
            if (mindcomp.Mind != null && mindcomp.Mind.TryGetSession(out var session))
            {
                //Zombie role for player manifest
                mindcomp.Mind.AddRole(new TraitorRole(mindcomp.Mind, _proto.Index<AntagPrototype>(zombiecomp.ZombieRoleId)));
                //Greeting message for new bebe zombers
                _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));
            }

            if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !mindcomp.HasMind) //this specific component gives build test trouble so pop off, ig
            {
                //yet more hardcoding. Visit zombie.ftl for more information.
                EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(target, out var ghostcomp);
                ghostcomp.RoleName = Loc.GetString("zombie-generic");
                ghostcomp.RoleDescription = Loc.GetString("zombie-role-desc");
                ghostcomp.RoleRules = Loc.GetString("zombie-role-rules");
            }

            ///Goes through every hand, drops the items in it, then removes the hand
            ///may become the source of various bugs.
            foreach (var hand in _sharedHands.EnumerateHands(target))
            {
                _sharedHands.SetActiveHand(target, hand);
                _sharedHands.DoDrop(target, hand);
                _sharedHands.RemoveHand(target, hand.Name);
            }
            RemComp<HandsComponent>(target);

            //zombie gamemode stuff
            RaiseLocalEvent(new EntityZombifiedEvent(target));
        }
    }
}
