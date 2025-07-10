using System.Collections.Generic;
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Content.Shared.Directions;
using Content.Shared.Damage;
using Content.Shared.Chemistry.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Robust.Shared.GameStates;

namespace Content.Shared._N14
{
    // Placeholder stub systems and events for ported RMC code.
    public sealed class RMCMapSystem : EntitySystem
    {
        public Direction[] CardinalDirections =
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West
        };

        public bool TryGetTileDef(EntityCoordinates coords, out ContentTileDefinition tile)
        {
            tile = default!;
            return false;
        }

        public bool HasAnchoredEntityEnumerator<T>(EntityCoordinates coords, out Entity<T> entity) where T : IComponent
        {
            entity = default!;
            return false;
        }

        public struct AnchoredEnumerator
        {
            public bool MoveNext(out EntityUid uid)
            {
                uid = default;
                return false;
            }
        }

        public AnchoredEnumerator GetAnchoredEntitiesEnumerator(EntityCoordinates coords)
        {
            return new AnchoredEnumerator();
        }
    }

    public sealed class SharedRMCMeleeWeaponSystem : EntitySystem
    {
        public void DoLunge(EntityUid user, EntityUid target) {}
    }

    public sealed class CMArmorSystem : EntitySystem
    {
        public void UpdateArmorValue((EntityUid, object?) value) {}
    }

    public sealed class XenoPlasmaSystem : EntitySystem
    {
        public void SetPlasma((EntityUid, object?) ent, float amount) {}
    }

    [RegisterComponent]
    public sealed partial class XenoPlasmaComponent : Component
    {
        public float Plasma;
        public float MaxPlasma;
    }

    public sealed class SharedRMCEmoteSystem : EntitySystem
    {
        public void TryEmoteWithChat(EntityUid entity, string emote) {}
    }

    public sealed class SharedOnCollideSystem : EntitySystem
    {
        public EntityUid SpawnChain() => EntityUid.Invalid;
        public void SetChain((EntityUid, DamageOnCollideComponent) ent, EntityUid chain) {}
    }

    [RegisterComponent]
    public sealed partial class DamageOnCollideComponent : Component
    {
        public DamageSpecifier Damage = new();
    }

    public sealed class SharedRMCSpraySystem : EntitySystem
    {
        public void Spray(EntityUid spray, EntityUid user, EntityCoordinates target) {}
    }

    public sealed class LineSystem : EntitySystem
    {
        public List<LineTile> DrawLine(EntityCoordinates from, EntityCoordinates to, TimeSpan delayPer, out object? dummy)
        {
            dummy = null;
            return new List<LineTile> { new() { Coordinates = to, At = TimeSpan.Zero } };
        }
    }

    [Serializable]
    public struct LineTile
    {
        public EntityCoordinates Coordinates;
        public TimeSpan At;
    }

    // Event placeholders
    public sealed class RMCTriggerEvent : EntityEventArgs {}
    public sealed class CMExplosiveTriggeredEvent : EntityEventArgs {}
    public sealed class DamageCollideEvent : EntityEventArgs
    {
        public EntityUid Target { get; set; }
    }

    public sealed class CMGetArmorEvent : EntityEventArgs
    {
        public float ArmorModifier = 1f;
    }

    public sealed class VaporHitEvent : EntityEventArgs
    {
        public Entity<SolutionComponent> Solution = default!;
    }

    public sealed class UniqueActionEvent : EntityEventArgs
    {
        public EntityUid UserUid { get; set; }
    }
}
