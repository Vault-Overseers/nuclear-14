using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._NC.Discord;
using Content.Server._NC.CCCvars;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._NC.Sponsors;

public sealed class SponsorsManager
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly DiscordAuthManager _discordAuthManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private ISawmill _sawmill = default!;

    private readonly HttpClient _httpClient = new();
    private string _guildId = default!;
    private string _apiUrl = default!;
    private string _apiKey = default!;

    private Dictionary<NetUserId, SponsorData> _cachedSponsors = new();

    public void Initialize()
    {
        _configuration.OnValueChanged(CCCVars.DiscordGuildID, s => _guildId = s, true);
        _configuration.OnValueChanged(CCCVars.DiscordApiUrl, s => _apiUrl = s, true);
        _configuration.OnValueChanged(CCCVars.ApiKey, value => _apiKey = value, true);

        _discordAuthManager.PlayerVerified += OnPlayerVerified;
        _netManager.Disconnect += OnDisconnect;

        _sawmill = Logger.GetSawmill("sponsors");
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedSponsors.Remove(e.Channel.UserId);
    }

    private async void OnPlayerVerified(object? sender, ICommonSession e)
    {
        var roles = await GetRoles(e.UserId);
        if (roles == null)
            return;

        var level = SponsorData.ParseRoles(roles);
        if (level == SponsorLevel.None)
            return;

        var data = new SponsorData(level, e.UserId);
        _cachedSponsors.Add(e.UserId, data);

        _sawmill.Info($"{e.UserId} is sponsor now.\nUserId: {e.UserId}. Level: {Enum.GetName(data.Level)}:{(int)data.Level}");
    }

    private async Task<List<string>?> GetRoles(NetUserId userId)
    {
        var requestUrl = $"{_apiUrl}/roles?userid={userId}&guildid={_guildId}&api_token={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error($"Failed to retrieve roles for user {userId}: {response.StatusCode}");
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        var rolesJson = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(responseContent);

        if (rolesJson != null && rolesJson.TryGetValue("roles", out var roles))
        {
            return roles;
        }

        _sawmill.Error($"Roles not found in response for user {userId}");
        return null;
    }

    public bool TryGetSponsorData(NetUserId userId, [NotNullWhen(true)] out SponsorData? sponsorData)
    {
        return _cachedSponsors.TryGetValue(userId, out sponsorData);
    }
}

public sealed class SponsorData
{
    public static readonly Dictionary<string, SponsorLevel> RolesMap = new()
    {
        { "1237790603601776710", SponsorLevel.Normal },
    };

    public static SponsorLevel ParseRoles(List<string> roles)
    {
        var highestRole = SponsorLevel.None;
        foreach (var role in roles)
        {
            if (RolesMap.ContainsKey(role))
                if ((int)RolesMap[role] > (int)highestRole)
                    highestRole = RolesMap[role];
        }

        return highestRole;
    }

    public SponsorData(SponsorLevel level, NetUserId userId)
    {
        Level = level;
        UserId = userId;
    }

    public SponsorLevel Level;
    public NetUserId UserId;
}

public enum SponsorLevel
{
    None = 0,
    Normal = 1,
}
