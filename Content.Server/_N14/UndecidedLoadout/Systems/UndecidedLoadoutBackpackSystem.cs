using Content.Server.UndecidedLoadout.Components;
using Content.Shared.Item;
using Content.Shared.UndecidedLoadout;
using Robust.Server.GameObjects;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.UndecidedLoadout.Systems;

/// <summary>
/// <see cref="UndecidedLoadoutBackpackComponent"/>
/// this system links the interface to the logic, and will output to the player a set of items selected by him in the interface
/// </summary>
public sealed class UndecidedLoadoutBackpackSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const int MaxSelectedSets = 1;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UndecidedLoadoutBackpackComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<UndecidedLoadoutBackpackComponent, UndecidedLoadoutBackpackApproveMessage>(OnApprove);
        SubscribeLocalEvent<UndecidedLoadoutBackpackComponent, UndecidedLoadoutBackpackChangeSetMessage>(OnChangeSet);
    }

    private void OnUIOpened(Entity<UndecidedLoadoutBackpackComponent> backpack, ref BoundUIOpenedEvent args)
    {
        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void OnApprove(Entity<UndecidedLoadoutBackpackComponent> backpack, ref UndecidedLoadoutBackpackApproveMessage args)
    {
        if (backpack.Comp.SelectedSets.Count != MaxSelectedSets)
            return;

        foreach (var i in backpack.Comp.SelectedSets)
        {
            var set = _proto.Index(backpack.Comp.PossibleSets[i]);
            foreach (var item in set.Content)
            {
                var ent = Spawn(item, _transform.GetMapCoordinates(backpack.Owner));
                if (TryComp<ItemComponent>(ent, out var itemComponent))
                    _transform.DropNextTo(ent, backpack.Owner);
            }
        }
        _audio.PlayPvs(backpack.Comp.ApproveSound, backpack.Owner);
        QueueDel(backpack);
    }
    private void OnChangeSet(Entity<UndecidedLoadoutBackpackComponent> backpack, ref UndecidedLoadoutBackpackChangeSetMessage args)
    {
        //Swith selecting set
        if (!backpack.Comp.SelectedSets.Remove(args.SetNumber))
            backpack.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void UpdateUI(EntityUid uid, UndecidedLoadoutBackpackComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<int, UndecidedLoadoutBackpackSetInfo> data = new();

        for (int i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new UndecidedLoadoutBackpackSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                selected);
            data.Add(i, info);
        }

        _ui.SetUiState(uid, UndecidedLoadoutBackpackUIKey.Key, new UndecidedLoadoutBackpackBoundUserInterfaceState(data, MaxSelectedSets));
    }
}
