using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Visuals;
using Content.Shared.Audio;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasVentPumpSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceUpdateEvent>(OnGasVentPumpUpdated);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceDisabledEvent>(OnGasVentPumpLeaveAtmosphere);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosDeviceEnabledEvent>(OnGasVentPumpEnterAtmosphere);
            SubscribeLocalEvent<GasVentPumpComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<GasVentPumpComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<GasVentPumpComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
            SubscribeLocalEvent<GasVentPumpComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasVentPumpComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<GasVentPumpComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnGasVentPumpUpdated(EntityUid uid, GasVentPumpComponent vent, AtmosDeviceUpdateEvent args)
        {
            //Bingo waz here
            if (vent.Welded)
            {
                return;
            }

            var nodeName = vent.PumpDirection switch
            {
                VentPumpDirection.Releasing => vent.Inlet,
                VentPumpDirection.Siphoning => vent.Outlet,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!vent.Enabled
                || !TryComp(uid, out AtmosDeviceComponent? device)
                || !TryComp(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(nodeName, out PipeNode? pipe))
            {
                return;
            }

            var environment = _atmosphereSystem.GetContainingMixture(uid, true, true);

            // We're in an air-blocked tile... Do nothing.
            if (environment == null)
            {
                return;
            }

            var timeDelta =  (_gameTiming.CurTime - device.LastProcess).TotalSeconds;
            var pressureDelta = (float) timeDelta * vent.TargetPressureChange;

            if (vent.PumpDirection == VentPumpDirection.Releasing && pipe.Air.Pressure > 0)
            {
                if (environment.Pressure > vent.MaxPressure)
                    return;

                if (environment.Pressure < vent.UnderPressureLockoutThreshold)
                {
                    vent.UnderPressureLockout = true;
                    return;
                }
                vent.UnderPressureLockout = false;

                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, vent.ExternalPressureBound - environment.Pressure);

                if (pressureDelta <= 0)
                    return;

                // how many moles to transfer to change external pressure by pressureDelta
                // (ignoring temperature differences because I am lazy)
                var transferMoles = pressureDelta * environment.Volume / (pipe.Air.Temperature * Atmospherics.R);

                // limit transferMoles so the source doesn't go below its bound.
                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                {
                    var internalDelta = pipe.Air.Pressure - vent.InternalPressureBound;

                    if (internalDelta <= 0)
                        return;

                    var maxTransfer = internalDelta * pipe.Air.Volume / (pipe.Air.Temperature * Atmospherics.R);
                    transferMoles = MathF.Min(transferMoles, maxTransfer);
                }

                _atmosphereSystem.Merge(environment, pipe.Air.Remove(transferMoles));
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning && environment.Pressure > 0)
            {
                if (pipe.Air.Pressure > vent.MaxPressure)
                    return;

                if ((vent.PressureChecks & VentPressureBound.InternalBound) != 0)
                    pressureDelta = MathF.Min(pressureDelta, vent.InternalPressureBound - pipe.Air.Pressure);

                if (pressureDelta <= 0)
                    return;

                // how many moles to transfer to change internal pressure by pressureDelta
                // (ignoring temperature differences because I am lazy)
                var transferMoles = pressureDelta * pipe.Air.Volume / (environment.Temperature * Atmospherics.R);

                // limit transferMoles so the source doesn't go below its bound.
                if ((vent.PressureChecks & VentPressureBound.ExternalBound) != 0)
                {
                    var externalDelta = environment.Pressure - vent.ExternalPressureBound;

                    if (externalDelta <= 0)
                        return;

                    var maxTransfer = externalDelta * environment.Volume / (environment.Temperature * Atmospherics.R);

                    transferMoles = MathF.Min(transferMoles, maxTransfer);
                }

                _atmosphereSystem.Merge(pipe.Air, environment.Remove(transferMoles));
            }
        }

        private void OnGasVentPumpLeaveAtmosphere(EntityUid uid, GasVentPumpComponent component, AtmosDeviceDisabledEvent args)
        {
            UpdateState(uid, component);
        }

        private void OnGasVentPumpEnterAtmosphere(EntityUid uid, GasVentPumpComponent component, AtmosDeviceEnabledEvent args)
        {
            UpdateState(uid, component);
        }

        private void OnAtmosAlarm(EntityUid uid, GasVentPumpComponent component, AtmosMonitorAlarmEvent args)
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

        private void OnPowerChanged(EntityUid uid, GasVentPumpComponent component, PowerChangedEvent args)
        {
            component.Enabled = args.Powered;
            UpdateState(uid, component);
        }

        private void OnPacketRecv(EntityUid uid, GasVentPumpComponent component, DeviceNetworkPacketEvent args)
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
                    if (!args.Data.TryGetValue(AirAlarmSystem.AirAlarmSetData, out GasVentPumpData? setData))
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

        private void OnInit(EntityUid uid, GasVentPumpComponent component, ComponentInit args)
        {
            if (component.CanLink)
                _signalSystem.EnsureReceiverPorts(uid, component.PressurizePort, component.DepressurizePort);
        }

        private void OnSignalReceived(EntityUid uid, GasVentPumpComponent component, SignalReceivedEvent args)
        {
            if (!component.CanLink)
                return;

            if (args.Port == component.PressurizePort)
            {
                component.PumpDirection = VentPumpDirection.Releasing;
                component.ExternalPressureBound = component.PressurizePressure;
                component.PressureChecks = VentPressureBound.ExternalBound;
                UpdateState(uid, component);
            }
            else if (args.Port == component.DepressurizePort)
            {
                component.PumpDirection = VentPumpDirection.Siphoning;
                component.ExternalPressureBound = component.DepressurizePressure;
                component.PressureChecks = VentPressureBound.ExternalBound;
                UpdateState(uid, component);
            }
        }

        private void UpdateState(EntityUid uid, GasVentPumpComponent vent, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref appearance, false))
                return;

            _ambientSoundSystem.SetAmbience(uid, true);
            if (!vent.Enabled)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                appearance.SetData(VentPumpVisuals.State, VentPumpState.Off);
            }
            else if (vent.PumpDirection == VentPumpDirection.Releasing)
            {
                appearance.SetData(VentPumpVisuals.State, VentPumpState.Out);
            }
            else if (vent.PumpDirection == VentPumpDirection.Siphoning)
            {
                appearance.SetData(VentPumpVisuals.State, VentPumpState.In);
            }
            else if (vent.Welded)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                appearance.SetData(VentPumpVisuals.State, VentPumpState.Welded);
            }
        }

        private void OnExamine(EntityUid uid, GasVentPumpComponent component, ExaminedEvent args)
        {
            if (!TryComp<GasVentPumpComponent>(uid, out var pumpComponent))
                return;
            if (args.IsInDetailsRange)
            {
                if (pumpComponent.UnderPressureLockout)
                {
                    args.PushMarkup(Loc.GetString("gas-vent-pump-uvlo"));
                }
            }
        }
    }
}
