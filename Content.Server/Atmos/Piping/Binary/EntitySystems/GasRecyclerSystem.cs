using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasReyclerSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;

        private const float MinTemp = 300 + Atmospherics.T0C; // 300 C
        private const float MinPressure = 30 * Atmospherics.OneAtmosphere;  // 3 MPa

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasRecyclerComponent, AtmosDeviceEnabledEvent>(OnEnabled);
            SubscribeLocalEvent<GasRecyclerComponent, AtmosDeviceUpdateEvent>(OnUpdate);
            SubscribeLocalEvent<GasRecyclerComponent, AtmosDeviceDisabledEvent>(OnDisabled);
            SubscribeLocalEvent<GasRecyclerComponent, ExaminedEvent>(OnExamined);
        }

        private void OnEnabled(EntityUid uid, GasRecyclerComponent comp, AtmosDeviceEnabledEvent args)
        {
            UpdateAppearance(uid, comp);
        }

        private void OnExamined(EntityUid uid, GasRecyclerComponent comp, ExaminedEvent args)
        {
            if (!EntityManager.GetComponent<TransformComponent>(comp.Owner).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
                return;

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(comp.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(comp.OutletName, out PipeNode? outlet))
            {
                return;
            }

            if (comp.Reacting)
            {
                args.PushMarkup(Loc.GetString("gas-recycler-reacting"));
            }
            else
            {
                if (inlet.Air.Pressure < MinPressure)
                {
                    args.PushMarkup(Loc.GetString("gas-recycler-low-pressure"));
                }

                if (inlet.Air.Temperature < MinTemp)
                {
                    args.PushMarkup(Loc.GetString("gas-recycler-low-temperature"));
                }
            }
        }

        private void OnUpdate(EntityUid uid, GasRecyclerComponent comp, AtmosDeviceUpdateEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(comp.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(comp.OutletName, out PipeNode? outlet))
            {
                _ambientSoundSystem.SetAmbience(comp.Owner, false);
                return;
            }

            // The gas recycler is a passive device, so it permits gas flow even if nothing is being reacted.
            comp.Reacting = inlet.Air.Temperature >= MinTemp && inlet.Air.Pressure >= MinPressure;
            var removed = inlet.Air.RemoveVolume(PassiveTransferVol(inlet.Air, outlet.Air));
            if (comp.Reacting)
            {
                var nCO2 = removed.GetMoles(Gas.CarbonDioxide);
                removed.AdjustMoles(Gas.CarbonDioxide, -nCO2);
                removed.AdjustMoles(Gas.Oxygen, nCO2);
                var nN2O = removed.GetMoles(Gas.NitrousOxide);
                removed.AdjustMoles(Gas.NitrousOxide, -nN2O);
                removed.AdjustMoles(Gas.Nitrogen, nN2O);
            }

            _atmosphereSystem.Merge(outlet.Air, removed);
            UpdateAppearance(uid, comp);
            _ambientSoundSystem.SetAmbience(comp.Owner, true);
        }

        public float PassiveTransferVol(GasMixture inlet, GasMixture outlet)
        {
            if (inlet.Pressure < outlet.Pressure)
            {
                return 0;
            }
            float overPressConst = 300; // pressure difference (in atm) to get 200 L/sec transfer rate
            float alpha = Atmospherics.MaxTransferRate / (float)Math.Sqrt(overPressConst*Atmospherics.OneAtmosphere);
            return alpha * (float)Math.Sqrt(inlet.Pressure - outlet.Pressure);
        }

        private void OnDisabled(EntityUid uid, GasRecyclerComponent comp, AtmosDeviceDisabledEvent args)
        {
            comp.Reacting = false;
            UpdateAppearance(uid, comp);
        }

        private void UpdateAppearance(EntityUid uid, GasRecyclerComponent? comp = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref comp, ref appearance, false))
                return;

            appearance.SetData(PumpVisuals.Enabled, comp.Reacting);
        }
    }
}
