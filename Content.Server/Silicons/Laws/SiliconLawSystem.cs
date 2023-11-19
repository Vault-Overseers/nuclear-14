﻿using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Roles;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SiliconLawBoundComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SiliconLawBoundComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedGetLaws);
        SubscribeLocalEvent<EmagSiliconLawComponent, GetSiliconLawsEvent>(OnDirectedEmagGetLaws);
        SubscribeLocalEvent<EmagSiliconLawComponent, MindAddedMessage>(OnEmagMindAdded);
        SubscribeLocalEvent<EmagSiliconLawComponent, MindRemovedMessage>(OnEmagMindRemoved);
        SubscribeLocalEvent<EmagSiliconLawComponent, ExaminedEvent>(OnExamined);
    }

    private void OnComponentStartup(EntityUid uid, SiliconLawBoundComponent component, ComponentStartup args)
    {
        component.ProvidedAction = new (_prototype.Index<InstantActionPrototype>(component.ViewLawsAction));
        _actions.AddAction(uid, component.ProvidedAction, null);
    }

    private void OnComponentShutdown(EntityUid uid, SiliconLawBoundComponent component, ComponentShutdown args)
    {
        if (component.ProvidedAction != null)
            _actions.RemoveAction(uid, component.ProvidedAction);
    }

    private void OnMapInit(EntityUid uid, SiliconLawBoundComponent component, MapInitEvent args)
    {
        GetLaws(uid, component);
    }

    private void OnMindAdded(EntityUid uid, SiliconLawBoundComponent component, MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false,
            actor.PlayerSession.ConnectedClient, colorOverride: Color.FromHex("#2ed2fd"));
    }

    private void OnToggleLawsScreen(EntityUid uid, SiliconLawBoundComponent component, ToggleLawsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, SiliconLawsUiKey.Key, actor.PlayerSession);
    }

    private void OnBoundUIOpened(EntityUid uid, SiliconLawBoundComponent component, BoundUIOpenedEvent args)
    {
        var state = new SiliconLawBuiState(GetLaws(uid));
        _userInterface.TrySetUiState(args.Entity, SiliconLawsUiKey.Key, state, (IPlayerSession) args.Session);
    }

    private void OnPlayerSpawnComplete(EntityUid uid, SiliconLawBoundComponent component, PlayerSpawnCompleteEvent args)
    {
        component.LastLawProvider = args.Station;
    }

    private void OnDirectedGetLaws(EntityUid uid, SiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || HasComp<EmaggedComponent>(uid) || component.Laws.Count == 0)
            return;

        foreach (var law in component.Laws)
        {
            args.Laws.Add(_prototype.Index<SiliconLawPrototype>(law));
        }

        args.Handled = true;
    }

    private void OnDirectedEmagGetLaws(EntityUid uid, EmagSiliconLawComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || !HasComp<EmaggedComponent>(uid) || component.OwnerName == null)
            return;

        args.Laws.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", component.OwnerName)),
            Order = 0
        });
    }

    private void OnExamined(EntityUid uid, EmagSiliconLawComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<EmaggedComponent>(uid))
            return;

        if (component.RequireOpenPanel && TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        args.PushMarkup(Loc.GetString("laws-compromised-examine"));
    }

    protected override void OnGotEmagged(EntityUid uid, EmagSiliconLawComponent component, ref GotEmaggedEvent args)
    {
        if (component.RequireOpenPanel && TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        base.OnGotEmagged(uid, component, ref args);
        NotifyLawsChanged(uid);
        EnsureEmaggedRole(uid, component);
    }

    private void OnEmagMindAdded(EntityUid uid, EmagSiliconLawComponent component, MindAddedMessage args)
    {
        if (HasComp<EmaggedComponent>(uid))
            EnsureEmaggedRole(uid, component);
    }

    private void OnEmagMindRemoved(EntityUid uid, EmagSiliconLawComponent component, MindRemovedMessage args)
    {
        if (component.AntagonistRole == null)
            return;

        if (args.OldMind.Roles.FirstOrDefault(r => r is SubvertedSiliconRole) is not { } role)
            return;

        _mind.RemoveRole(args.OldMind, role);
    }

    private void EnsureEmaggedRole(EntityUid uid, EmagSiliconLawComponent component)
    {
        if (component.AntagonistRole == null || !_mind.TryGetMind(uid, out var mind))
            return;

        if (_mind.HasRole<SubvertedSiliconRole>(mind))
            return;
        _mind.AddRole(mind, new SubvertedSiliconRole(mind, _prototype.Index<AntagPrototype>(component.AntagonistRole)));
    }

    public List<SiliconLaw> GetLaws(EntityUid uid, SiliconLawBoundComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new List<SiliconLaw>();

        var ev = new GetSiliconLawsEvent(uid);

        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
        {
            component.LastLawProvider = uid;
            return ev.Laws;
        }

        var xform = Transform(uid);

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = station;
                return ev.Laws;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = grid;
                return ev.Laws;
            }
        }

        if (component.LastLawProvider == null ||
            Deleted(component.LastLawProvider) ||
            Terminating(component.LastLawProvider.Value))
        {
            component.LastLawProvider = null;
        }
        else
        {
            RaiseLocalEvent(component.LastLawProvider.Value, ref ev);
            if (ev.Handled)
            {
                return ev.Laws;
            }
        }

        RaiseLocalEvent(ref ev);
        return ev.Laws;
    }

    public void NotifyLawsChanged(EntityUid uid)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.ConnectedClient, colorOverride: Color.Red);
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class LawsCommand : ToolshedCommand
{
    private SiliconLawSystem? _law;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<SiliconLawBoundComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<string> Get([PipedArgument] EntityUid lawbound)
    {
        _law ??= GetSys<SiliconLawSystem>();

        foreach (var law in _law.GetLaws(lawbound))
        {
            yield return $"law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}";
        }
    }
}
