using System.Numerics;
using Content.Shared._NC.CameraFollow.Components; // NC changes
using Content.Shared.Actions; // NC changes
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera;

[UsedImplicitly]
public abstract class SharedCameraRecoilSystem : EntitySystem
{
    /// <summary>
    ///     Maximum rate of magnitude restore towards 0 kick.
    /// </summary>
    private const float RestoreRateMax = 30f;

    /// <summary>
    ///     Minimum rate of magnitude restore towards 0 kick.
    /// </summary>
    private const float RestoreRateMin = 0.1f;

    /// <summary>
    ///     Time in seconds since the last kick that lerps RestoreRateMin and RestoreRateMax
    /// </summary>
    private const float RestoreRateRamp = 4f;

    /// <summary>
    ///     The maximum magnitude of the kick applied to the camera at any point.
    /// </summary>
    protected const float KickMagnitudeMax = 1f;

    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!; // NC changes

    /// <summary>
    ///     Applies explosion/recoil/etc kickback to the view of the entity.
    /// </summary>
    /// <remarks>
    ///     If the entity is missing <see cref="CameraRecoilComponent" /> and/or <see cref="EyeComponent" />,
    ///     this call will have no effect. It is safe to call this function on any entity.
    /// </remarks>

    /// NC changes start
    public override void Initialize()
    {
        SubscribeLocalEvent<CameraFollowComponent, ComponentInit>(OnCameraFollowInit);
    }
    /// NC changes end
    public abstract void KickCamera(EntityUid euid, Vector2 kickback, CameraRecoilComponent? component = null);

    public override void Update(float frameTime) // NC changes
    {
        base.FrameUpdate(frameTime);

        var query = AllEntityQuery<EyeComponent, CameraRecoilComponent, CameraFollowComponent>(); // NC changes

        while (query.MoveNext(out var uid, out var eye, out var recoil, out var follow)) // NC changes
        {
            var magnitude = recoil.CurrentKick.Length();
            if (magnitude <= 0.005f)
            {
                recoil.CurrentKick = Vector2.Zero;
                var offset = recoil.BaseOffset + recoil.CurrentKick + (follow.Enabled ? follow.Offset : Vector2.Zero); // NC-Changes
                _eye.SetOffset(uid, offset, eye); // NC-Changes
            }
            else // Continually restore camera to 0.
            {
                var normalized = recoil.CurrentKick.Normalized();
                recoil.LastKickTime += frameTime;
                var restoreRate = MathHelper.Lerp(RestoreRateMin, RestoreRateMax, Math.Min(1, recoil.LastKickTime / RestoreRateRamp));
                var restore = normalized * restoreRate * frameTime;
                var (x, y) = recoil.CurrentKick - restore;
                if (Math.Sign(x) != Math.Sign(recoil.CurrentKick.X)) x = 0;

                if (Math.Sign(y) != Math.Sign(recoil.CurrentKick.Y)) y = 0;

                recoil.CurrentKick = new Vector2(x, y);

                var offset = recoil.BaseOffset + recoil.CurrentKick + (follow.Enabled ? follow.Offset : Vector2.Zero); // NC-Changes
                _eye.SetOffset(uid, offset, eye); // NC changes
            }
        }
    }

    /// NC changes start
    private void OnCameraFollowInit(EntityUid uid, CameraFollowComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnCameraFollowRemove(EntityUid uid, CameraFollowComponent component, ComponentRemove args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionEntity);
    }
    /// NC changes end
}

[Serializable]
[NetSerializable]
public sealed class CameraKickEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly Vector2 Recoil;

    public CameraKickEvent(NetEntity netEntity, Vector2 recoil)
    {
        Recoil = recoil;
        NetEntity = netEntity;
    }
}
