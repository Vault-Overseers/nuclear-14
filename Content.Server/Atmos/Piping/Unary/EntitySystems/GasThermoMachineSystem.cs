using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Construction;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasThermoMachineSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceUpdateEvent>(OnThermoMachineUpdated);
            SubscribeLocalEvent<GasThermoMachineComponent, AtmosDeviceDisabledEvent>(OnThermoMachineLeaveAtmosphere);
            SubscribeLocalEvent<GasThermoMachineComponent, RefreshPartsEvent>(OnGasThermoRefreshParts);

            // UI events
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineToggleMessage>(OnToggleMessage);
            SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineChangeTemperatureMessage>(OnChangeTemperature);
        }

        private void OnThermoMachineUpdated(EntityUid uid, GasThermoMachineComponent thermoMachine, AtmosDeviceUpdateEvent args)
        {
            if (!thermoMachine.Enabled
                || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(thermoMachine.InletName, out PipeNode? inlet))
            {
                DirtyUI(uid, thermoMachine);
                _appearance.SetData(uid, ThermoMachineVisuals.Enabled, false);
                return;
            }

            var airHeatCapacity = _atmosphereSystem.GetHeatCapacity(inlet.Air);
            var combinedHeatCapacity = airHeatCapacity + thermoMachine.HeatCapacity;

            if (!MathHelper.CloseTo(combinedHeatCapacity, 0, 0.001f))
            {
                _appearance.SetData(uid, ThermoMachineVisuals.Enabled, true);
                var combinedEnergy = thermoMachine.HeatCapacity * thermoMachine.TargetTemperature + airHeatCapacity * inlet.Air.Temperature;
                inlet.Air.Temperature = combinedEnergy / combinedHeatCapacity;
            }

            // TODO ATMOS: Active power usage.
        }

        private void OnThermoMachineLeaveAtmosphere(EntityUid uid, GasThermoMachineComponent component, AtmosDeviceDisabledEvent args)
        {
            _appearance.SetData(uid, ThermoMachineVisuals.Enabled, false);

            DirtyUI(uid, component);
        }

        private void OnGasThermoRefreshParts(EntityUid uid, GasThermoMachineComponent component, RefreshPartsEvent args)
        {
            var matterBinRating = args.PartRatings[component.MachinePartHeatCapacity];
            var laserRating = args.PartRatings[component.MachinePartTemperature];

            component.HeatCapacity = 5000 * MathF.Pow(matterBinRating, 2);

            switch (component.Mode)
            {
                // 593.15K with stock parts.
                case ThermoMachineMode.Heater:
                    component.MaxTemperature = component.BaseMaxTemperature + component.MaxTemperatureDelta * laserRating;
                    component.MinTemperature = Atmospherics.T20C;
                    break;
                // 73.15K with stock parts.
                case ThermoMachineMode.Freezer:
                    component.MinTemperature = MathF.Max(
                        component.BaseMinTemperature - component.MinTemperatureDelta * laserRating, Atmospherics.TCMB);
                    component.MaxTemperature = Atmospherics.T20C;
                    break;
            }

            DirtyUI(uid, component);
        }

        private void OnToggleMessage(EntityUid uid, GasThermoMachineComponent component, GasThermomachineToggleMessage args)
        {
            component.Enabled = !component.Enabled;

            DirtyUI(uid, component);
        }

        private void OnChangeTemperature(EntityUid uid, GasThermoMachineComponent component, GasThermomachineChangeTemperatureMessage args)
        {
            component.TargetTemperature =
                Math.Clamp(args.Temperature, component.MinTemperature, component.MaxTemperature);

            DirtyUI(uid, component);
        }

        private void DirtyUI(EntityUid uid, GasThermoMachineComponent? thermo, ServerUserInterfaceComponent? ui=null)
        {
            if (!Resolve(uid, ref thermo, ref ui, false))
                return;

            _userInterfaceSystem.TrySetUiState(uid, ThermomachineUiKey.Key,
                new GasThermomachineBoundUserInterfaceState(thermo.MinTemperature, thermo.MaxTemperature, thermo.TargetTemperature, thermo.Enabled, thermo.Mode), null, ui);
        }
    }
}
