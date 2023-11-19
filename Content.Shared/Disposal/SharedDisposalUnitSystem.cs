﻿using Content.Shared.Body.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Disposal;

[Serializable, NetSerializable]
public sealed class DisposalDoAfterEvent : SimpleDoAfterEvent
{
}

public abstract class SharedDisposalUnitSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly MetaDataSystem Metadata = default!;
    [Dependency] private   readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly SharedJointSystem Joints = default!;

    protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

    // Percentage
    public const float PressurePerSecond = 0.05f;

    /// <summary>
    /// Gets the current pressure state of a disposals unit.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public DisposalsPressureState GetState(EntityUid uid, SharedDisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        var nextPressure = Metadata.GetPauseTime(uid, metadata) + component.NextPressurized - GameTiming.CurTime;
        var pressurizeTime = 1f / PressurePerSecond;
        var pressurizeDuration = pressurizeTime - component.FlushDelay.TotalSeconds;

        if (nextPressure.TotalSeconds > pressurizeDuration)
        {
            return DisposalsPressureState.Flushed;
        }

        if (nextPressure > TimeSpan.Zero)
        {
            return DisposalsPressureState.Pressurizing;
        }

        return DisposalsPressureState.Ready;
    }

    public float GetPressure(EntityUid uid, SharedDisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        if (!Resolve(uid, ref metadata))
            return 0f;

        var pauseTime = Metadata.GetPauseTime(uid, metadata);
        return MathF.Min(1f,
            (float) (GameTiming.CurTime - pauseTime - component.NextPressurized).TotalSeconds / PressurePerSecond);
    }

    protected void OnPreventCollide(EntityUid uid, SharedDisposalUnitComponent component,
        ref PreventCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        // Items dropped shouldn't collide but items thrown should
        if (EntityManager.HasComponent<ItemComponent>(otherBody) &&
            !EntityManager.HasComponent<ThrownItemComponent>(otherBody))
        {
            args.Cancelled = true;
            return;
        }

        if (component.RecentlyEjected.Contains(otherBody))
        {
            args.Cancelled = true;
        }
    }

    protected void OnCanDragDropOn(EntityUid uid, SharedDisposalUnitComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanInsert(uid, component, args.Dragged);
        args.Handled = true;
    }

    protected void OnEmagged(EntityUid uid, SharedDisposalUnitComponent component, ref GotEmaggedEvent args)
    {
        component.DisablePressure = true;
        args.Handled = true;
    }

    public virtual bool CanInsert(EntityUid uid, SharedDisposalUnitComponent component, EntityUid entity)
    {
        if (!EntityManager.GetComponent<TransformComponent>(uid).Anchored)
            return false;

        // TODO: Probably just need a disposable tag.
        if (!EntityManager.TryGetComponent(entity, out ItemComponent? storable) &&
            !EntityManager.HasComponent<BodyComponent>(entity))
        {
            return false;
        }

        //Check if the entity is a mob and if mobs can be inserted
        if (TryComp<MobStateComponent>(entity, out var damageState) && !component.MobsCanEnter)
            return false;

        if (EntityManager.TryGetComponent(entity, out PhysicsComponent? physics) &&
            (physics.CanCollide || storable != null))
        {
            return true;
        }

        return damageState != null && (!component.MobsCanEnter || _mobState.IsDead(entity, damageState));
    }

    /// <summary>
    /// TODO: Proper prediction
    /// </summary>
    public abstract void DoInsertDisposalUnit(EntityUid uid, EntityUid toInsert, EntityUid user, SharedDisposalUnitComponent? disposal = null);

    [Serializable, NetSerializable]
    protected sealed class DisposalUnitComponentState : ComponentState
    {
        public SoundSpecifier? FlushSound;
        public DisposalsPressureState State;
        public TimeSpan NextPressurized;
        public TimeSpan AutomaticEngageTime;
        public TimeSpan? NextFlush;
        public bool Powered;
        public bool Engaged;
        public List<EntityUid> RecentlyEjected;

        public DisposalUnitComponentState(SoundSpecifier? flushSound, DisposalsPressureState state, TimeSpan nextPressurized, TimeSpan automaticEngageTime, TimeSpan? nextFlush, bool powered, bool engaged, List<EntityUid> recentlyEjected)
        {
            FlushSound = flushSound;
            State = state;
            NextPressurized = nextPressurized;
            AutomaticEngageTime = automaticEngageTime;
            NextFlush = nextFlush;
            Powered = powered;
            Engaged = engaged;
            RecentlyEjected = recentlyEjected;
        }
    }
}
