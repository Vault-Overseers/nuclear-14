# Tools

This folder contains various utility scripts used to maintain and build the project.

## Discord Bridge Setup

The repository provides a built-in Discord bridge that can relay chat between the game server and one or more Discord channels. To use it you need to configure several [CVars](../Content.Shared/CCVar) either in your `config.toml` or via environment variables.

### Required Settings

| CVar | Description |
|------|-------------|
| `discord.token` | The bot token used to authenticate with Discord. Create a bot at <https://discord.com/developers/applications> and copy its token here. Keep this value secret. |
| `discord.guild_id` | The numeric ID of the Discord guild (server) your bot will operate in. You can copy this by enabling "Developer Mode" in Discord and using "Copy ID" on your server. |
| `discord.prefix` | Command prefix the bot uses when listening for commands. Defaults to `!`. |
| `ooc.discord_channel_id` | Channel ID where OOC messages are sent and received. |
| `admin.chat_discord_channel_id` | Channel ID for admin chat relay. |
| `deadchat.discord_channel_id` | Channel ID that relays dead chat. |

All channel and guild IDs are snowflakes -- 64-bit unsigned integers shown as strings in Discord's UI. The bot must have permission to read and send messages in these channels.

### Enabling the Bridge

1. Set `discord.token` and `discord.guild_id` in your server configuration.
2. Specify the Discord channel IDs for the chats you want to bridge.
3. Launch the server. On startup the `DiscordLink` service will connect to Discord and start forwarding messages.

If any of the required values are missing, the bridge will simply not start, and the server will continue running without Discord integration.

