using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Radio.Components;
using Content.Server.Power.Components;
using Content.Shared._N14.Communications;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server._N14.Communications
{
public sealed partial class RadioTowerSystem : SharedRadioTowerSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioTowerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RadioTowerComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RadioTowerComponent, EncryptionChannelsChangedEvent>(OnEncryptionChanged);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnStartup(EntityUid uid, RadioTowerComponent component, ComponentStartup args)
    {
        if (TryComp<EncryptionKeyHolderComponent>(uid, out var holder))
            UpdateChannels(uid, component, holder);
    }

    private void OnEncryptionChanged(EntityUid uid, RadioTowerComponent component, ref EncryptionChannelsChangedEvent args)
    {
        if (uid != args.Holder)
            return;

        UpdateChannels(uid, component, args.Component);
    }

    private void UpdateChannels(EntityUid uid, RadioTowerComponent comp, EncryptionKeyHolderComponent holder)
    {
        comp.Channels.Clear();
        comp.Channels.UnionWith(holder.Channels.Select(id => new ProtoId<RadioChannelPrototype>(id)));
        Dirty(comp);
    }

    private void OnInteractHand(EntityUid uid, RadioTowerComponent comp, InteractHandEvent args)
    {
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var power) || !power.Powered)
        {
            _popup.PopupClient(Loc.GetString("radiotower-no-power"), uid, args.User);
            return;
        }

        comp.Status = comp.Status == RadioTowerStatus.On ? RadioTowerStatus.Off : RadioTowerStatus.On;
        Dirty(uid, comp);
        _popup.PopupClient(Loc.GetString(comp.Status == RadioTowerStatus.On ? "radiotower-on" : "radiotower-off"), uid, args.User);
        _adminLog.Add(LogType.Action, $"{ToPrettyString(args.User):user} toggled radio tower {ToPrettyString(uid):tower} to {comp.Status}");
    }

    private bool HasTower(MapId mapId, ProtoId<RadioChannelPrototype> channel)
    {
        var query = EntityQueryEnumerator<RadioTowerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tower, out var xform))
        {
            if (xform.MapID != mapId)
                continue;
            if (tower.Status != RadioTowerStatus.On)
                continue;
            if (!tower.Channels.Contains(channel))
                continue;
            if (!TryComp<ApcPowerReceiverComponent>(uid, out var power) || !power.Powered)
                continue;
            return true;
        }

        var backQuery = EntityQueryEnumerator<RadioBackpackComponent, TransformComponent>();
        while (backQuery.MoveNext(out var uid, out var backpack, out var bxform))
        {
            if (bxform.MapID != mapId)
                continue;
            if (!backpack.Channels.Contains(channel))
                continue;
            return true;
        }

        return false;
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent ev)
    {
        var senderMap = Transform(ev.RadioSource).MapID;
        var receiverMap = Transform(ev.RadioReceiver).MapID;
        if (senderMap == receiverMap)
            return;

        var proto = ev.Channel;
        if (!proto.TowerRequired)
            return;

        if (!HasTower(senderMap, proto.ID) || !HasTower(receiverMap, proto.ID))
            ev.Cancelled = true;
    }
}
}
