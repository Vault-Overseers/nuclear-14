using Content.Server.Radio.Components;
using Content.Shared._N14.Communications;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._N14.Communications
{
public sealed partial class RadioBackpackSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioBackpackComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RadioBackpackComponent, EncryptionChannelsChangedEvent>(OnEncryptionChanged);
    }

    private void OnStartup(EntityUid uid, RadioBackpackComponent component, ComponentStartup args)
    {
        if (TryComp<EncryptionKeyHolderComponent>(uid, out var holder))
            UpdateChannels(component, holder);
    }

    private void OnEncryptionChanged(EntityUid uid, RadioBackpackComponent component, ref EncryptionChannelsChangedEvent args)
    {
        UpdateChannels(component, args.Component);
    }

    private void UpdateChannels(RadioBackpackComponent comp, EncryptionKeyHolderComponent holder)
    {
        comp.Channels.Clear();
        comp.Channels.UnionWith(holder.Channels.Select(id => new ProtoId<RadioChannelPrototype>(id)));
    }
}
}
