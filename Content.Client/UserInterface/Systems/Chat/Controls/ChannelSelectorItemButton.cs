﻿using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorItemButton : Button
{
    public readonly ChatSelectChannel Channel;

    public bool IsHidden => Parent == null;

    public ChannelSelectorItemButton(ChatSelectChannel selector)
    {
        Channel = selector;
        AddStyleClass(StyleNano.StyleClassChatChannelSelectorButton);
        Text = ChatUIController.GetChannelSelectorName(selector);
        var prefix = ChatUIController.GetChannelSelectorPrefix(selector);
        if (prefix != default) Text = Loc.GetString("hud-chatbox-select-name-prefixed", ("name", Text), ("prefix", prefix));
    }
}
