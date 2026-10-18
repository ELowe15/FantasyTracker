using YahooApiConnector.Services.Interfaces;

namespace YahooApiConnector.Services;

public class RosterService : IRosterService
{
    private readonly YahooFantasyApiClient _apiClient;
    private readonly IPlayerImageService _imageService;

    public RosterService(YahooFantasyApiClient apiClient, IPlayerImageService imageService)
    {
        _apiClient = apiClient;
        _imageService = imageService;
    }

    public async Task<List<TeamRoster>> GetTeamRostersForDateAsync(string leagueKey, DateTime date)
    {
        var teams = await _apiClient.GetLeagueTeamsAsync(leagueKey);
        var nsTeamsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.LeagueTeams(leagueKey));
        var ns = YahooFantasyXmlParser.GetNamespace(nsTeamsDoc);

        var teamRosters = new List<TeamRoster>();

        foreach (var team in teams)
        {
            var rosterDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.TeamRoster(team.TeamKey, date));
            var nsR = YahooFantasyXmlParser.GetNamespace(rosterDoc);

            var players = rosterDoc.Descendants(nsR + "player")
                .Select(p =>
                {
                    var rawPlayerKey = p.Element(nsR + "player_key")?.Value;
                    var hashedKey = Helpers.Hash(rawPlayerKey ?? string.Empty);
                    var imageUrl = p.Element(nsR + "image_url")?.Value;

                    _imageService.TryAdd(hashedKey, imageUrl);

                    return new Player
                    {
                        PlayerKey = hashedKey,
                        FullName = p.Element(nsR + "name")?.Element(nsR + "full")?.Value,
                        Position = p.Element(nsR + "display_position")?.Value,
                        NbaTeam = p.Element(nsR + "editorial_team_abbr")?.Value
                    };
                })
                .ToList();

            teamRosters.Add(new TeamRoster
            {
                TeamKey = Helpers.Hash(team.TeamKey),
                ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
                Players = players
            });
        }

        return teamRosters;
    }

    public async Task DumpAllTeamRostersToJsonAsync(string leagueKey, string outputPath)
    {
        var teamsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.LeagueTeams(leagueKey));
        var ns = YahooFantasyXmlParser.GetNamespace(teamsDoc);

        var teams = teamsDoc.Descendants(ns + "team")
            .Select(t => new
            {
                TeamKey = t.Element(ns + "team_key")?.Value,
                ManagerName = t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.TeamKey))
            .ToList();

        var allRosters = new List<TeamRoster>();
        await _imageService.LoadAsync();

        foreach (var team in teams)
        {
            var rosterDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.TeamRoster(team.TeamKey!));
            var nsR = YahooFantasyXmlParser.GetNamespace(rosterDoc);

            var players = rosterDoc.Descendants(nsR + "player")
                .Select(p =>
                {
                    var rawPlayerKey = p.Element(nsR + "player_key")?.Value;
                    var hashedKey = Helpers.Hash(rawPlayerKey ?? string.Empty);
                    var imageUrl = p.Element(nsR + "image_url")?.Value;

                    _imageService.TryAdd(hashedKey, imageUrl);

                    return new Player
                    {
                        PlayerKey = hashedKey,
                        FullName = p.Element(nsR + "name")?.Element(nsR + "full")?.Value,
                        Position = p.Element(nsR + "display_position")?.Value,
                        NbaTeam = p.Element(nsR + "editorial_team_abbr")?.Value
                    };
                })
                .ToList();

            allRosters.Add(new TeamRoster
            {
                TeamKey = Helpers.Hash(team.TeamKey!),
                ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
                Players = players
            });
        }

        await _imageService.SaveAsync();
        await YahooFantasyPersistence.WriteJsonFileAsync(outputPath, allRosters);
    }
}