using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Pinpointer
{
    public sealed class ClientPinpointerSystem : SharedPinpointerSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PinpointerComponent, ComponentHandleState>(HandleCompState);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            // we want to show pinpointers arrow direction relative
            // to players eye rotation (like it was in SS13)

            // because eye can change it rotation anytime
            // we need to update this arrow in a update loop
            foreach (var pinpointer in EntityQuery<PinpointerComponent>())
            {
                UpdateAppearance(pinpointer.Owner, pinpointer);
                UpdateEyeDir(pinpointer.Owner, pinpointer);
            }
        }

        private void HandleCompState(EntityUid uid, PinpointerComponent pinpointer, ref ComponentHandleState args)
        {
            if (args.Current is not PinpointerComponentState state)
                return;

            SetActive(uid, state.IsActive, pinpointer);
            SetDirection(uid, state.DirectionToTarget, pinpointer);
            SetDistance(uid, state.DistanceToTarget, pinpointer);
        }

        private void UpdateAppearance(EntityUid uid, PinpointerComponent? pinpointer = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref pinpointer, ref appearance))
                return;

            _appearance.SetData(uid, PinpointerVisuals.IsActive, pinpointer.IsActive, appearance);
            _appearance.SetData(uid, PinpointerVisuals.TargetDistance, pinpointer.DistanceToTarget, appearance);
        }

        private void UpdateDirAppearance(EntityUid uid, Direction dir,PinpointerComponent? pinpointer = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref pinpointer, ref appearance))
                return;

            _appearance.SetData(uid, PinpointerVisuals.TargetDirection, dir, appearance);
        }

        /// <summary>
        ///     Transform pinpointer arrow from world space to eye space
        ///     And send it to the appearance component
        /// </summary>
        private void UpdateEyeDir(EntityUid uid, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return;

            var worldDir = pinpointer.DirectionToTarget;
            if (worldDir == Direction.Invalid)
            {
                UpdateDirAppearance(uid, Direction.Invalid, pinpointer);
                return;
            }

            var eye = _eyeManager.CurrentEye;
            var angle = worldDir.ToAngle() + eye.Rotation;
            var eyeDir = angle.GetDir();
            UpdateDirAppearance(uid, eyeDir, pinpointer);
        }
    }
}
