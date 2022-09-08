using Content.Server.CombatMode;
using Content.Server.Interaction;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.AI.Operators.Combat.Melee
{
    public sealed class UnarmedCombatOperator : AiOperator
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private readonly float _burstTime;
        private float _elapsedTime;

        private readonly EntityUid _owner;
        private readonly EntityUid _target;
        private MeleeWeaponComponent? _unarmedCombat;

        public UnarmedCombatOperator(EntityUid owner, EntityUid target, float burstTime = 1.0f)
        {
            IoCManager.InjectDependencies(this);

            _owner = owner;
            _target = target;
            _burstTime = burstTime;
        }

        public override bool Startup()
        {
            if (!base.Startup())
            {
                return true;
            }

            if (!_entMan.TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
            {
                return false;
            }

            if (!combatModeComponent.IsInCombatMode)
            {
                combatModeComponent.IsInCombatMode = true;
            }

            if (_entMan.TryGetComponent(_owner, out MeleeWeaponComponent? unarmedCombatComponent))
            {
                _unarmedCombat = unarmedCombatComponent;
            }
            else
            {
                return false;
            }

            return true;
        }

        public override bool Shutdown(Outcome outcome)
        {
            if (!base.Shutdown(outcome))
                return false;

            if (_entMan.TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
            {
                combatModeComponent.IsInCombatMode = false;
            }

            return true;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_unarmedCombat == null ||
                !_entMan.GetComponent<TransformComponent>(_target).Coordinates.TryDistance(_entMan, _entMan.GetComponent<TransformComponent>(_owner).Coordinates, out var distance) || distance >
                _unarmedCombat.Range)
            {
                return Outcome.Failed;
            }

            if (_burstTime <= _elapsedTime)
            {
                return Outcome.Success;
            }

            if (_unarmedCombat?.Deleted ?? true)
            {
                return Outcome.Failed;
            }

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            interactionSystem.AiUseInteraction(_owner, _entMan.GetComponent<TransformComponent>(_target).Coordinates, _target);
            _elapsedTime += frameTime;
            return Outcome.Continuing;
        }
    }
}
