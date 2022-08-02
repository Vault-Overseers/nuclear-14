using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasVentScrubberSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceUpdateEvent>(OnVentScrubberUpdated);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceEnabledEvent>(OnVentScrubberEnterAtmosphere);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceDisabledEvent>(OnVentScrubberLeaveAtmosphere);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<GasVentScrubberComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<GasVentScrubberComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        }

        private void OnVentScrubberUpdated(EntityUid uid, GasVentScrubberComponent scrubber, AtmosDeviceUpdateEvent args)
        {
            if (scrubber.Welded)
            {
                return;
            }

            if (!TryComp(uid, out AtmosDeviceComponent? device))
                return;

            var timeDelta = (float) (_gameTiming.CurTime - device.LastProcess).TotalSeconds;

            if (!scrubber.Enabled
            || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            || !nodeContainer.TryGetNode(scrubber.OutletName, out PipeNode? outlet))
                return;

            var xform = Transform(uid);

            if (xform.GridUid == null)
                return;

            var position = _transformSystem.GetGridOrMapTilePosition(uid, xform);

            var environment = _atmosphereSystem.GetTileMixture(xform.GridUid, xform.MapUid, position, true);

            Scrub(timeDelta, scrubber, environment, outlet);

            if (!scrubber.WideNet)
                return;

            // Scrub adjacent tiles too.
            foreach (var adjacent in _atmosphereSystem.GetAdjacentTileMixtures(xform.GridUid.Value, position, false, true))
            {
                Scrub(timeDelta, scrubber, adjacent, outlet);
            }
        }

        private void OnVentScrubberLeaveAtmosphere(EntityUid uid, GasVentScrubberComponent component,
            AtmosDeviceDisabledEvent args) => UpdateState(uid, component);

        private void OnVentScrubberEnterAtmosphere(EntityUid uid, GasVentScrubberComponent component,
            AtmosDeviceEnabledEvent args) => UpdateState(uid, component);

        private void Scrub(float timeDelta, GasVentScrubberComponent scrubber, GasMixture? tile, PipeNode outlet)
        {
            Scrub(timeDelta, scrubber.TransferRate, scrubber.PumpDirection, scrubber.FilterGases, tile, outlet.Air);
        }

        /// <summary>
        /// True if we were able to scrub, false if we were not.
        /// </summary>
        public bool Scrub(float timeDelta, float transferRate, ScrubberPumpDirection mode, HashSet<Gas> filterGases, GasMixture? tile, GasMixture destination)
        {
            // Cannot scrub if tile is null or air-blocked.
            if (tile == null
                || destination.Pressure >= 50 * Atmospherics.OneAtmosphere) // Cannot scrub if pressure too high.
            {
                return false;
            }

            // Take a gas sample.
            var ratio = MathF.Min(1f, timeDelta * transferRate / tile.Volume);
            var removed = tile.RemoveRatio(ratio);

            // Nothing left to remove from the tile.
            if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
                return false;

            if (mode == ScrubberPumpDirection.Scrubbing)
            {
                _atmosphereSystem.ScrubInto(removed, destination, filterGases);

                // Remix the gases.
                _atmosphereSystem.Merge(tile, removed);
            }
            else if (mode == ScrubberPumpDirection.Siphoning)
            {
                _atmosphereSystem.Merge(destination, removed);
            }
            return true;
        }

        private void OnAtmosAlarm(EntityUid uid, GasVentScrubberComponent component, AtmosMonitorAlarmEvent args)
        {
            if (args.HighestNetworkType == AtmosMonitorAlarmType.Danger)
            {
                component.Enabled = false;
            }
            else if (args.HighestNetworkType == AtmosMonitorAlarmType.Normal)
            {
                component.Enabled = true;
            }

            UpdateState(uid, component);
        }

        private void OnPowerChanged(EntityUid uid, GasVentScrubberComponent component, PowerChangedEvent args)
        {
            component.Enabled = args.Powered;
            UpdateState(uid, component);
        }

        private void OnPacketRecv(EntityUid uid, GasVentScrubberComponent component, DeviceNetworkPacketEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? netConn)
                || !EntityManager.TryGetComponent(uid, out AtmosAlarmableComponent? alarmable)
                || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
                return;

            var payload = new NetworkPayload();

            switch (cmd)
            {
                case AirAlarmSystem.AirAlarmSyncCmd:
                    payload.Add(DeviceNetworkConstants.Command, AirAlarmSystem.AirAlarmSyncData);
                    payload.Add(AirAlarmSystem.AirAlarmSyncData, component.ToAirAlarmData());

                    _deviceNetSystem.QueuePacket(uid, args.SenderAddress, payload, device: netConn);

                    return;
                case AirAlarmSystem.AirAlarmSetData:
                    if (!args.Data.TryGetValue(AirAlarmSystem.AirAlarmSetData, out GasVentScrubberData? setData))
                        break;

                    component.FromAirAlarmData(setData);
                    UpdateState(uid, component);
                    alarmable.IgnoreAlarms = setData.IgnoreAlarms;
                    payload.Add(DeviceNetworkConstants.Command, AirAlarmSystem.AirAlarmSetDataStatus);
                    payload.Add(AirAlarmSystem.AirAlarmSetDataStatus, true);

                    _deviceNetSystem.QueuePacket(uid, null, payload, device: netConn);

                    return;
            }
        }

        /// <summary>
        ///     Updates a scrubber's appearance and ambience state.
        /// </summary>
        private void UpdateState(EntityUid uid, GasVentScrubberComponent scrubber,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref appearance, false))
                return;

            _ambientSoundSystem.SetAmbience(uid, true);
            if (!scrubber.Enabled)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Off);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Scrubbing)
            {
                appearance.SetData(ScrubberVisuals.State, scrubber.WideNet ? ScrubberState.WideScrub : ScrubberState.Scrub);
            }
            else if (scrubber.PumpDirection == ScrubberPumpDirection.Siphoning)
            {
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Siphon);
            }
            else if (scrubber.Welded)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Welded);
            }
        }
    }
}
