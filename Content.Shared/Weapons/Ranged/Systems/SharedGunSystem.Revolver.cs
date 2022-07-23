using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    protected const string RevolverContainer = "revolver-ammo";

    protected virtual void InitializeRevolver()
    {
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentGetState>(OnRevolverGetState);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentHandleState>(OnRevolverHandleState);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentInit>(OnRevolverInit);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, TakeAmmoEvent>(OnRevolverTakeAmmo);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, GetVerbsEvent<Verb>>(OnRevolverVerbs);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, InteractUsingEvent>(OnRevolverInteractUsing);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, GetAmmoCountEvent>(OnRevolverGetAmmoCount);
    }

    private void OnRevolverGetAmmoCount(EntityUid uid, RevolverAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count += GetRevolverCount(component);
        args.Capacity += component.Capacity;
    }

    private void OnRevolverInteractUsing(EntityUid uid, RevolverAmmoProviderComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (TryRevolverInsert(component, args.Used, args.User))
            args.Handled = true;
    }

    private void OnRevolverGetState(EntityUid uid, RevolverAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new RevolverAmmoProviderComponentState
        {
            CurrentIndex = component.CurrentIndex,
            AmmoSlots = component.AmmoSlots,
            Chambers = component.Chambers,
        };
    }

    private void OnRevolverHandleState(EntityUid uid, RevolverAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RevolverAmmoProviderComponentState state) return;

        var oldIndex = component.CurrentIndex;
        component.CurrentIndex = state.CurrentIndex;
        component.Chambers = new bool?[state.Chambers.Length];

        // Need to copy across the state rather than the ref.
        for (var i = 0; i < component.AmmoSlots.Count; i++)
        {
            component.AmmoSlots[i] = state.AmmoSlots[i];
            component.Chambers[i] = state.Chambers[i];
        }

        // Handle spins
        if (Timing.IsFirstTimePredicted)
        {
            if (oldIndex != state.CurrentIndex)
                UpdateAmmoCount(uid);
        }
    }

    public bool TryRevolverInsert(RevolverAmmoProviderComponent component, EntityUid uid, EntityUid? user)
    {
        if (component.Whitelist?.IsValid(uid, EntityManager) == false) return false;

        for (var i = 0; i < component.Capacity; i++)
        {
            var index = (component.CurrentIndex + i) % component.Capacity;

            if (component.AmmoSlots[index] != null ||
                component.Chambers[index] != null) continue;

            component.AmmoSlots[index] = uid;
            component.AmmoContainer.Insert(uid);
            PlaySound(component.Owner, component.SoundInsert?.GetSound(Random, ProtoManager), user);
            Popup(Loc.GetString("gun-revolver-insert"), component.Owner, user);
            UpdateRevolverAppearance(component);
            UpdateAmmoCount(uid);
            Dirty(component);
            return true;
        }

        Popup(Loc.GetString("gun-revolver-full"), component.Owner, user);
        return false;
    }

    private void OnRevolverVerbs(EntityUid uid, RevolverAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null) return;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("gun-revolver-empty"),
            Disabled = !AnyRevolverCartridges(component),
            Act = () => EmptyRevolver(component, args.User)
        });

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("gun-revolver-spin"),
            // Category = VerbCategory.G,
            Act = () => SpinRevolver(component, args.User)
        });
    }

    private bool AnyRevolverCartridges(RevolverAmmoProviderComponent component)
    {
        for (var i = 0; i < component.Capacity; i++)
        {
            if (component.Chambers[i] != null ||
                component.AmmoSlots[i] != null) return true;
        }

        return false;
    }

    private int GetRevolverCount(RevolverAmmoProviderComponent component)
    {
        var count = 0;

        for (var i = 0; i < component.Capacity; i++)
        {
            if (component.Chambers[i] != null ||
                component.AmmoSlots[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private int GetRevolverUnspentCount(RevolverAmmoProviderComponent component)
    {
        var count = 0;

        for (var i = 0; i < component.Capacity; i++)
        {
            var chamber = component.Chambers[i];

            if (chamber == true)
            {
                count++;
                continue;
            }

            var ammo = component.AmmoSlots[i];

            if (TryComp<CartridgeAmmoComponent>(ammo, out var cartridge) && !cartridge.Spent)
            {
                count++;
            }
        }

        return count;
    }

    public void EmptyRevolver(RevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        var xform = Transform(component.Owner);
        var mapCoordinates = xform.MapPosition;
        var anyEmpty = false;

        for (var i = 0; i < component.Capacity; i++)
        {
            var chamber = component.Chambers[i];
            var slot = component.AmmoSlots[i];

            if (slot == null)
            {
                if (chamber == null) continue;

                // Too lazy to make a new method don't sue me.
                if (!_netManager.IsClient)
                {
                    var uid = Spawn(component.FillPrototype, mapCoordinates);

                    if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
                        SetCartridgeSpent(cartridge, !(bool) chamber);

                    EjectCartridge(uid);
                }

                component.Chambers[i] = null;
                anyEmpty = true;
            }
            else
            {
                component.AmmoSlots[i] = null;
                component.AmmoContainer.Remove(slot.Value);

                if (!_netManager.IsClient)
                    EjectCartridge(slot.Value);

                anyEmpty = true;
            }
        }

        if (anyEmpty)
        {
            PlaySound(component.Owner, component.SoundEject?.GetSound(Random, ProtoManager), user);
            UpdateAmmoCount(component.Owner);
            UpdateRevolverAppearance(component);
            Dirty(component);
        }
    }

    private void UpdateRevolverAppearance(RevolverAmmoProviderComponent component)
    {
        if (!TryComp<AppearanceComponent>(component.Owner, out var appearance)) return;
        var count = GetRevolverCount(component);
        appearance.SetData(AmmoVisuals.HasAmmo, count != 0);
        appearance.SetData(AmmoVisuals.AmmoCount, count);
        appearance.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    protected virtual void SpinRevolver(RevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        PlaySound(component.Owner, component.SoundSpin?.GetSound(Random, ProtoManager), user);
        Popup(Loc.GetString("gun-revolver-spun"), component.Owner, user);
    }

    private void OnRevolverTakeAmmo(EntityUid uid, RevolverAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var currentIndex = component.CurrentIndex;
        Cycle(component, args.Shots);

        // Revolvers provide the bullets themselves rather than the cartridges so they stay in the revolver.
        for (var i = 0; i < args.Shots; i++)
        {
            var index = (currentIndex + i) % component.Capacity;
            var chamber = component.Chambers[index];

            // Get unspawned ent first if possible.
            if (chamber != null)
            {
                if (chamber == true)
                {
                    // TODO: This is kinda sussy boy
                    var ent = Spawn(component.FillPrototype, args.Coordinates);

                    if (TryComp<CartridgeAmmoComponent>(ent, out var cartridge))
                    {
                        component.Chambers[index] = false;
                        SetCartridgeSpent(cartridge, true);
                        args.Ammo.Add(EnsureComp<AmmoComponent>(Spawn(cartridge.Prototype, args.Coordinates)));
                        Del(ent);
                        continue;
                    }

                    component.Chambers[i] = null;
                    args.Ammo.Add(EnsureComp<AmmoComponent>(ent));
                }
            }
            else if (component.AmmoSlots[index] != null)
            {
                var ent = component.AmmoSlots[index]!;

                if (TryComp<CartridgeAmmoComponent>(ent, out var cartridge))
                {
                    if (cartridge.Spent) continue;

                    SetCartridgeSpent(cartridge, true);
                    args.Ammo.Add(EnsureComp<AmmoComponent>(Spawn(cartridge.Prototype, args.Coordinates)));
                    continue;
                }

                component.AmmoContainer.Remove(ent.Value);
                component.AmmoSlots[index] = null;
                args.Ammo.Add(EnsureComp<AmmoComponent>(ent.Value));
                Transform(ent.Value).Coordinates = args.Coordinates;
            }
        }

        UpdateRevolverAppearance(component);
        Dirty(component);
    }

    private void Cycle(RevolverAmmoProviderComponent component, int count = 1)
    {
        component.CurrentIndex = (component.CurrentIndex + count) % component.Capacity;
    }

    private void OnRevolverInit(EntityUid uid, RevolverAmmoProviderComponent component, ComponentInit args)
    {
        component.AmmoContainer = Containers.EnsureContainer<Container>(uid, RevolverContainer);
        component.AmmoSlots.EnsureCapacity(component.Capacity);
        var remainder = component.Capacity - component.AmmoSlots.Count;

        for (var i = 0; i < remainder; i++)
        {
            component.AmmoSlots.Add(null);
        }

        component.Chambers = new bool?[component.Capacity];

        if (component.FillPrototype != null)
        {
            for (var i = 0; i < component.Capacity; i++)
            {
                if (component.AmmoSlots[i] != null)
                {
                    component.Chambers[i] = null;
                    continue;
                }

                component.Chambers[i] = true;
            }
        }

        DebugTools.Assert(component.AmmoSlots.Count == component.Capacity);
    }

    [Serializable, NetSerializable]
    protected sealed class RevolverAmmoProviderComponentState : ComponentState
    {
        public int CurrentIndex;
        public List<EntityUid?> AmmoSlots = default!;
        public bool?[] Chambers = default!;
    }

    public sealed class RevolverSpinEvent : EntityEventArgs
    {

    }
}
