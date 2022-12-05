using System.Linq;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using JetBrains.Annotations;
using Content.Shared.Power;
using Robust.Server.GameObjects;

namespace Content.Server.Power.EntitySystems
{
    /// <summary>
    ///     Manages power networks, power state, and all power components.
    /// </summary>
    [UsedImplicitly]
    public sealed class PowerNetSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        private readonly PowerState _powerState = new();
        private readonly HashSet<PowerNet> _powerNetReconnectQueue = new();
        private readonly HashSet<ApcNet> _apcNetReconnectQueue = new();

        private readonly BatteryRampPegSolver _solver = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(NodeGroupSystem));

            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentInit>(ApcPowerReceiverInit);
            SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentShutdown>(ApcPowerReceiverShutdown);
            SubscribeLocalEvent<ApcPowerReceiverComponent, EntityPausedEvent>(ApcPowerReceiverPaused);
            SubscribeLocalEvent<PowerNetworkBatteryComponent, ComponentInit>(BatteryInit);
            SubscribeLocalEvent<PowerNetworkBatteryComponent, ComponentShutdown>(BatteryShutdown);
            SubscribeLocalEvent<PowerNetworkBatteryComponent, EntityPausedEvent>(BatteryPaused);
            SubscribeLocalEvent<PowerConsumerComponent, ComponentInit>(PowerConsumerInit);
            SubscribeLocalEvent<PowerConsumerComponent, ComponentShutdown>(PowerConsumerShutdown);
            SubscribeLocalEvent<PowerConsumerComponent, EntityPausedEvent>(PowerConsumerPaused);
            SubscribeLocalEvent<PowerSupplierComponent, ComponentInit>(PowerSupplierInit);
            SubscribeLocalEvent<PowerSupplierComponent, ComponentShutdown>(PowerSupplierShutdown);
            SubscribeLocalEvent<PowerSupplierComponent, EntityPausedEvent>(PowerSupplierPaused);
        }

        private void ApcPowerReceiverInit(EntityUid uid, ApcPowerReceiverComponent component, ComponentInit args)
        {
            AllocLoad(component.NetworkLoad);
        }

        private void ApcPowerReceiverShutdown(EntityUid uid, ApcPowerReceiverComponent component,
            ComponentShutdown args)
        {
            _powerState.Loads.Free(component.NetworkLoad.Id);
        }

        private static void ApcPowerReceiverPaused(
            EntityUid uid,
            ApcPowerReceiverComponent component,
            EntityPausedEvent args)
        {
            component.NetworkLoad.Paused = args.Paused;
        }

        private void BatteryInit(EntityUid uid, PowerNetworkBatteryComponent component, ComponentInit args)
        {
            AllocBattery(component.NetworkBattery);
        }

        private void BatteryShutdown(EntityUid uid, PowerNetworkBatteryComponent component, ComponentShutdown args)
        {
            _powerState.Batteries.Free(component.NetworkBattery.Id);
        }

        private static void BatteryPaused(EntityUid uid, PowerNetworkBatteryComponent component, EntityPausedEvent args)
        {
            component.NetworkBattery.Paused = args.Paused;
        }

        private void PowerConsumerInit(EntityUid uid, PowerConsumerComponent component, ComponentInit args)
        {
            AllocLoad(component.NetworkLoad);
        }

        private void PowerConsumerShutdown(EntityUid uid, PowerConsumerComponent component, ComponentShutdown args)
        {
            _powerState.Loads.Free(component.NetworkLoad.Id);
        }

        private static void PowerConsumerPaused(EntityUid uid, PowerConsumerComponent component, EntityPausedEvent args)
        {
            component.NetworkLoad.Paused = args.Paused;
        }

        private void PowerSupplierInit(EntityUid uid, PowerSupplierComponent component, ComponentInit args)
        {
            AllocSupply(component.NetworkSupply);
        }

        private void PowerSupplierShutdown(EntityUid uid, PowerSupplierComponent component, ComponentShutdown args)
        {
            _powerState.Supplies.Free(component.NetworkSupply.Id);
        }

        private static void PowerSupplierPaused(EntityUid uid, PowerSupplierComponent component, EntityPausedEvent args)
        {
            component.NetworkSupply.Paused = args.Paused;
        }

        public void InitPowerNet(PowerNet powerNet)
        {
            AllocNetwork(powerNet.NetworkNode);
        }

        public void DestroyPowerNet(PowerNet powerNet)
        {
            _powerState.Networks.Free(powerNet.NetworkNode.Id);
        }

        public void QueueReconnectPowerNet(PowerNet powerNet)
        {
            _powerNetReconnectQueue.Add(powerNet);
        }

        public void InitApcNet(ApcNet apcNet)
        {
            AllocNetwork(apcNet.NetworkNode);
        }

        public void DestroyApcNet(ApcNet apcNet)
        {
            _powerState.Networks.Free(apcNet.NetworkNode.Id);
        }

        public void QueueReconnectApcNet(ApcNet apcNet)
        {
            _apcNetReconnectQueue.Add(apcNet);
        }

        public PowerStatistics GetStatistics()
        {
            return new()
            {
                CountBatteries = _powerState.Batteries.Count,
                CountLoads = _powerState.Loads.Count,
                CountNetworks = _powerState.Networks.Count,
                CountSupplies = _powerState.Supplies.Count
            };
        }

        public NetworkPowerStatistics GetNetworkStatistics(PowerState.Network network)
        {
            // Right, consumption. Now this is a big mess.
            // Start by summing up consumer draw rates.
            // Then deal with batteries.
            // While for consumers we want to use their max draw rates,
            //  for batteries we ought to use their current draw rates,
            //  because there's all sorts of weirdness with them.
            // A full battery will still have the same max draw rate,
            //  but will likely have deliberately limited current draw rate.
            float consumptionW = network.Loads.Sum(s => _powerState.Loads[s].DesiredPower);
            consumptionW += network.BatteriesCharging.Sum(s => _powerState.Batteries[s].CurrentReceiving);

            // This is interesting because LastMaxSupplySum seems to match LastAvailableSupplySum for some reason.
            // I suspect it's accounting for current supply rather than theoretical supply.
            float maxSupplyW = network.Supplies.Sum(s => _powerState.Supplies[s].MaxSupply);

            // Battery stuff is more complex.
            // Without stealing PowerState, the most efficient way
            //  to grab the necessary discharge data is from
            //  PowerNetworkBatteryComponent (has Pow3r reference).
            float supplyBatteriesW = 0.0f;
            float storageCurrentJ = 0.0f;
            float storageMaxJ = 0.0f;
            foreach (var discharger in network.BatteriesDischarging)
            {
                var nb = _powerState.Batteries[discharger];
                supplyBatteriesW += nb.CurrentSupply;
                storageCurrentJ += nb.CurrentStorage;
                storageMaxJ += nb.Capacity;
                maxSupplyW += nb.MaxSupply;
            }
            // And charging
            float outStorageCurrentJ = 0.0f;
            float outStorageMaxJ = 0.0f;
            foreach (var charger in network.BatteriesCharging)
            {
                var nb = _powerState.Batteries[charger];
                outStorageCurrentJ += nb.CurrentStorage;
                outStorageMaxJ += nb.Capacity;
            }
            return new()
            {
                SupplyCurrent = network.LastMaxSupplySum,
                SupplyBatteries = supplyBatteriesW,
                SupplyTheoretical = maxSupplyW,
                Consumption = consumptionW,
                InStorageCurrent = storageCurrentJ,
                InStorageMax = storageMaxJ,
                OutStorageCurrent = outStorageCurrentJ,
                OutStorageMax = outStorageMaxJ
            };
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Reconnect networks.
            {
                foreach (var apcNet in _apcNetReconnectQueue)
                {
                    if (apcNet.Removed)
                        continue;

                    DoReconnectApcNet(apcNet);
                }

                _apcNetReconnectQueue.Clear();

                foreach (var powerNet in _powerNetReconnectQueue)
                {
                    if (powerNet.Removed)
                        continue;

                    DoReconnectPowerNet(powerNet);
                }

                _powerNetReconnectQueue.Clear();
            }

            // Synchronize batteries
            RaiseLocalEvent(new NetworkBatteryPreSync());

            // Run power solver.
            _solver.Tick(frameTime, _powerState);

            // Synchronize batteries, the other way around.
            RaiseLocalEvent(new NetworkBatteryPostSync());

            // Send events where necessary.
            // TODO: Instead of querying ALL power components every tick, and then checking if an event needs to be
            // raised, should probably assemble a list of entity Uids during the actual solver steps.
            {
                var appearanceQuery = GetEntityQuery<AppearanceComponent>();
                foreach (var apcReceiver in EntityManager.EntityQuery<ApcPowerReceiverComponent>())
                {
                    var powered = apcReceiver.Powered;
                    if (powered == apcReceiver.PoweredLastUpdate)
                        continue;

                    apcReceiver.PoweredLastUpdate = powered;
                    var ev = new PowerChangedEvent(apcReceiver.Powered, apcReceiver.NetworkLoad.ReceivingPower);

                    RaiseLocalEvent(apcReceiver.Owner, ref ev);

                    if (appearanceQuery.TryGetComponent(apcReceiver.Owner, out var appearance))
                        _appearance.SetData(appearance.Owner, PowerDeviceVisuals.Powered, powered, appearance);
                }

                foreach (var consumer in EntityManager.EntityQuery<PowerConsumerComponent>())
                {
                    var newRecv = consumer.NetworkLoad.ReceivingPower;
                    ref var lastRecv = ref consumer.LastReceived;
                    if (MathHelper.CloseToPercent(lastRecv, newRecv))
                        continue;

                    lastRecv = newRecv;
                    var msg = new PowerConsumerReceivedChanged(newRecv, consumer.DrawRate);
                    RaiseLocalEvent(consumer.Owner, ref msg);
                }

                foreach (var powerNetBattery in EntityManager.EntityQuery<PowerNetworkBatteryComponent>())
                {
                    var lastSupply = powerNetBattery.LastSupply;
                    var currentSupply = powerNetBattery.CurrentSupply;

                    if (lastSupply == 0f && currentSupply != 0f)
                    {
                        var ev = new PowerNetBatterySupplyEvent(true);
                        RaiseLocalEvent(powerNetBattery.Owner, ref ev);
                    }
                    else if (lastSupply > 0f && currentSupply == 0f)
                    {
                        var ev = new PowerNetBatterySupplyEvent(false);
                        RaiseLocalEvent(powerNetBattery.Owner, ref ev);
                    }

                    powerNetBattery.LastSupply = currentSupply;
                }
            }
        }

        private void AllocLoad(PowerState.Load load)
        {
            _powerState.Loads.Allocate(out load.Id) = load;
        }

        private void AllocSupply(PowerState.Supply supply)
        {
            _powerState.Supplies.Allocate(out supply.Id) = supply;
        }

        private void AllocBattery(PowerState.Battery battery)
        {
            _powerState.Batteries.Allocate(out battery.Id) = battery;
        }

        private void AllocNetwork(PowerState.Network network)
        {
            _powerState.Networks.Allocate(out network.Id) = network;
        }

        private void DoReconnectApcNet(ApcNet net)
        {
            var netNode = net.NetworkNode;

            netNode.Loads.Clear();
            netNode.BatteriesDischarging.Clear();
            netNode.BatteriesCharging.Clear();
            netNode.Supplies.Clear();

            foreach (var provider in net.Providers)
            {
                foreach (var receiver in provider.LinkedReceivers)
                {
                    netNode.Loads.Add(receiver.NetworkLoad.Id);
                    receiver.NetworkLoad.LinkedNetwork = netNode.Id;
                }
            }

            foreach (var consumer in net.Consumers)
            {
                netNode.Loads.Add(consumer.NetworkLoad.Id);
                consumer.NetworkLoad.LinkedNetwork = netNode.Id;
            }

            var batteryQuery = GetEntityQuery<PowerNetworkBatteryComponent>();

            foreach (var apc in net.Apcs)
            {
                var netBattery = batteryQuery.GetComponent(apc.Owner);
                netNode.BatteriesDischarging.Add(netBattery.NetworkBattery.Id);
                netBattery.NetworkBattery.LinkedNetworkDischarging = netNode.Id;
            }
        }

        private void DoReconnectPowerNet(PowerNet net)
        {
            var netNode = net.NetworkNode;

            netNode.Loads.Clear();
            netNode.Supplies.Clear();
            netNode.BatteriesCharging.Clear();
            netNode.BatteriesDischarging.Clear();

            foreach (var consumer in net.Consumers)
            {
                netNode.Loads.Add(consumer.NetworkLoad.Id);
                consumer.NetworkLoad.LinkedNetwork = netNode.Id;
            }

            foreach (var supplier in net.Suppliers)
            {
                netNode.Supplies.Add(supplier.NetworkSupply.Id);
                supplier.NetworkSupply.LinkedNetwork = netNode.Id;
            }

            var batteryQuery = GetEntityQuery<PowerNetworkBatteryComponent>();

            foreach (var charger in net.Chargers)
            {
                var battery = batteryQuery.GetComponent(charger.Owner);
                netNode.BatteriesCharging.Add(battery.NetworkBattery.Id);
                battery.NetworkBattery.LinkedNetworkCharging = netNode.Id;
            }

            foreach (var discharger in net.Dischargers)
            {
                var battery = batteryQuery.GetComponent(discharger.Owner);
                netNode.BatteriesDischarging.Add(battery.NetworkBattery.Id);
                battery.NetworkBattery.LinkedNetworkDischarging = netNode.Id;
            }
        }
    }

    /// <summary>
    ///     Raised before power network simulation happens, to synchronize battery state from
    ///     components like <see cref="BatteryComponent"/> into <see cref="PowerNetworkBatteryComponent"/>.
    /// </summary>
    public readonly struct NetworkBatteryPreSync
    {
    }

    /// <summary>
    ///     Raised after power network simulation happens, to synchronize battery charge changes from
    ///     <see cref="PowerNetworkBatteryComponent"/> to components like <see cref="BatteryComponent"/>.
    /// </summary>
    public readonly struct NetworkBatteryPostSync
    {
    }

    /// <summary>
    ///     Raised when the amount of receiving power on a <see cref="PowerConsumerComponent"/> changes.
    /// </summary>
    [ByRefEvent]
    public readonly record struct PowerConsumerReceivedChanged(float ReceivedPower, float DrawRate)
    {
        public readonly float ReceivedPower = ReceivedPower;
        public readonly float DrawRate = DrawRate;
    }

    /// <summary>
    /// Raised whenever a <see cref="PowerNetworkBatteryComponent"/> changes from / to 0 CurrentSupply.
    /// </summary>
    [ByRefEvent]
    public readonly record struct PowerNetBatterySupplyEvent(bool Supply)
    {
        public readonly bool Supply = Supply;
    }

    public struct PowerStatistics
    {
        public int CountNetworks;
        public int CountLoads;
        public int CountSupplies;
        public int CountBatteries;
    }

    public struct NetworkPowerStatistics
    {
        public float SupplyCurrent;
        public float SupplyBatteries;
        public float SupplyTheoretical;
        public float Consumption;
        public float InStorageCurrent;
        public float InStorageMax;
        public float OutStorageCurrent;
        public float OutStorageMax;
    }

}
