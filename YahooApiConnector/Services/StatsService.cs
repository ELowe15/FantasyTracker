using YahooApiConnector.Services.Interfaces;

namespace YahooApiConnector.Services;

    public class StatsService : IStatsService
    {
        private readonly YahooFantasyApiClient _apiClient;
        private readonly IRosterService _rosterService;

        public StatsService(YahooFantasyApiClient apiClient, IRosterService rosterService)
        {
            _apiClient = apiClient;
            _rosterService = rosterService;
        }

        public async Task<Dictionary<string, List<WeeklyPlayerStats>>> GetDailyLeagueResultsAsync(string leagueKey, DateTime date, bool debugStopAfterFirstTeam = false)
        {
            var teamRosters = await _rosterService.GetTeamRostersForDateAsync(leagueKey, date);
            var allPlayerKeys = teamRosters
                .SelectMany(t => t.Players ?? new List<Player>())
                .Select(p => p.PlayerKey)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct()
                .ToList();

            // Note: player keys here are hashed in roster service. We need raw keys for API calls.
            // However roster service hashed them; original service used raw keys. To remain compatible,
            // call back to API to get rosters with raw keys when batching. Simpler: build player key list by fetching rosters via API directly.

            // Rebuild list of raw keys by querying team roster endpoints again
            var rawPlayerKeys = new List<string>();
            var teams = await _apiClient.GetLeagueTeamsAsync(leagueKey);
            foreach (var team in teams)
            {
                var rosterDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.TeamRoster(team.TeamKey, date));
                var ns = YahooFantasyXmlParser.GetNamespace(rosterDoc);

                var keys = rosterDoc.Descendants(ns + "player")
                    .Select(p => p.Element(ns + "player_key")?.Value)
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Select(k => k!)
                    .ToList();

                rawPlayerKeys.AddRange(keys);
            }

            rawPlayerKeys = rawPlayerKeys.Distinct().ToList();

            const int batchSize = 25;
            var dailyStats = new Dictionary<string, WeeklyPlayerStats>();

            for (var i = 0; i < rawPlayerKeys.Count; i += batchSize)
            {
                var batchKeys = rawPlayerKeys.Skip(i).Take(batchSize).ToList();
                var statsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.PlayersStatsByDate(batchKeys, date));
                var ns = YahooFantasyXmlParser.GetNamespace(statsDoc);

                foreach (var playerNode in statsDoc.Descendants(ns + "player"))
                {
                    var rawKey = playerNode.Element(ns + "player_key")?.Value;
                    if (string.IsNullOrWhiteSpace(rawKey))
                        continue;

                    var parsed = YahooFantasyXmlParser.ParsePlayerNodeToWeeklyPlayerStats(playerNode, ns);
                    if (parsed == null)
                        continue;

                    dailyStats[rawKey] = parsed;
                }

                if (debugStopAfterFirstTeam)
                    break;
            }

            var result = new Dictionary<string, List<WeeklyPlayerStats>>();
            foreach (var team in teamRosters)
            {
                var teamHashedKey = team.TeamKey ?? string.Empty;
                var teamPlayerStats = new List<WeeklyPlayerStats>();

                foreach (var player in team.Players ?? new List<Player>())
                {
                    // player's PlayerKey here is hashed; need to find corresponding raw key in dailyStats by matching hashed value
                    var match = dailyStats.Values.FirstOrDefault(ds => ds.PlayerKey == player.PlayerKey);
                    if (match != null)
                        teamPlayerStats.Add(match);
                }

                result[teamHashedKey] = teamPlayerStats;

                if (debugStopAfterFirstTeam)
                    break;
            }

            return result;
        }

        public async Task<List<WeeklyPlayerStats>> GetFirstTeamAllPlayerStatsForDateAsync(string leagueKey, DateTime date)
        {
        var results = new List<WeeklyPlayerStats>();
        var teamsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.LeagueTeamsWithPlayers(leagueKey));
        var ns = YahooFantasyXmlParser.GetNamespace(teamsDoc);
        var teams = teamsDoc.Descendants(ns + "team").ToList();

        if (teams.Count < 2)
        {
            Console.WriteLine("Second team not found.");
            return results;
        }

        var secondTeam = teams[1];
        var playerKeys = secondTeam
            .Descendants(ns + "player")
            .Select(p => p.Element(ns + "player_key")?.Value)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!)
            .ToList();

        foreach (var playerKey in playerKeys)
        {
            var statsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.PlayerStatsByDate(playerKey, date));
            var playerNode = statsDoc.Descendants(ns + "player").FirstOrDefault();
            if (playerNode == null)
                continue;

            var player = YahooFantasyXmlParser.ParsePlayerNodeToWeeklyPlayerStats(playerNode, ns);
            if (player != null)
                results.Add(player);
        }

        return results;
    }

    public async Task<List<TeamWeeklyStats>> GetWeeklyTeamStatsAsync(string leagueKey)
    {
        var teams = await _apiClient.GetLeagueTeamsAsync(leagueKey);
        var effectiveDate = DateTime.UtcNow.Date.AddDays(-1);
        var week = 1; // week parameter will be resolved by caller; keep simple here

        var results = new List<TeamWeeklyStats>();
        foreach (var team in teams)
        {
            var statsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.TeamStats(team.TeamKey, week));
            var ns = YahooFantasyXmlParser.GetNamespace(statsDoc);

            var statDict = statsDoc.Descendants(ns + "stat")
                .Where(s => s.Element(ns + "stat_id") != null)
                .ToDictionary(
                    s => Helpers.GetStatDisplayName(s.Element(ns + "stat_id")?.Value ?? string.Empty),
                    s => s.Element(ns + "value")?.Value ?? "0",
                    StringComparer.OrdinalIgnoreCase);

            results.Add(new TeamWeeklyStats
            {
                TeamKey = Helpers.Hash(team.TeamKey),
                ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
                StatValues = statDict
            });
        }

        return results;
    }
}