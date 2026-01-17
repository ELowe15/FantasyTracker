using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

public class YahooFantasyService
{
    private readonly HttpClient _client;

    public YahooFantasyService(string accessToken)
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }



    private async Task<int> GetSeasonAsync(string leagueKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/settings";
        var xml = await _client.GetStringAsync(url);

        var doc = XDocument.Parse(xml);
        var ns = doc.Root.GetDefaultNamespace();

        var seasonStr = doc.Descendants(ns + "season").FirstOrDefault()?.Value;

        return int.TryParse(seasonStr, out int season)
            ? season
            : DateTime.Now.Year; // safe fallback
    }


    public async Task<(DateTime start, DateTime end)> GetWeekDateRangeAsync(
        string leagueKey,
        int week)
    {
        var url =
            $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/scoreboard;week={week}";

        var xml = await _client.GetStringAsync(url);
        var doc = XDocument.Parse(xml);
        var ns = doc.Root.GetDefaultNamespace();

        var startStr = doc.Descendants(ns + "week_start").FirstOrDefault()?.Value;
        var endStr = doc.Descendants(ns + "week_end").FirstOrDefault()?.Value;

        DateTime.TryParse(startStr, out DateTime start);
        DateTime.TryParse(endStr, out DateTime end);

        return (start, end);
    }

    public async Task WriteLeagueContextAsync(int season, int currentWeek, string outputDirectory)
    {
        // Default to current directory if blank
        if (string.IsNullOrWhiteSpace(outputDirectory))
            outputDirectory = Directory.GetCurrentDirectory();

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var availableWeeks = GetAvailableWeeks(season, outputDirectory);

        var context = new LeagueContext
        {
            Season = season,
            CurrentWeek = currentWeek,
            AvailableWeeks = availableWeeks
        };

        var json = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
        var outputPath = Path.Combine(outputDirectory, "league_context.json");

        await File.WriteAllTextAsync(outputPath, json);
    }

    private List<int> GetAvailableWeeks(int season, string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            directoryPath = Directory.GetCurrentDirectory();

        if (!Directory.Exists(directoryPath))
            return new List<int>();

        var files = Directory.GetFiles(directoryPath, $"best_ball_{season}_week_*.json", SearchOption.TopDirectoryOnly);

        return files.Select(f =>
        {
            var match = Regex.Match(Path.GetFileName(f), $@"best_ball_{season}_week_(\d+)\.json");
            return match.Success ? int.Parse(match.Groups[1].Value) : (int?)null;
        })
        .Where(w => w.HasValue)
        .Select(w => w!.Value)
        .Distinct()
        .OrderBy(w => w)
        .ToList();
    }

    public async Task<int> GetWeekForDateAsync(string leagueKey, DateTime date)
    {
        int currentWeek = await GetCurrentWeekAsync(leagueKey);

        // Check current and previous week only (cheap & safe)
        for (int w = currentWeek; w >= Math.Max(1, currentWeek - 1); w--)
        {
            var (start, end) = await GetWeekDateRangeAsync(leagueKey, w);

            if (date.Date >= start.Date && date.Date <= end.Date)
                return w;
        }

        // Fallback: assume last completed week
        return Math.Max(1, currentWeek - 1);
    }


    public async Task<int> GetCurrentWeekAsync(string leagueKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}";
        var resp = await _client.GetAsync(url);
        var xml = await resp.Content.ReadAsStringAsync();

        var doc = XDocument.Parse(xml);
        XNamespace ns = doc.Root.GetDefaultNamespace();

        var weekStr = doc.Descendants(ns + "current_week").FirstOrDefault()?.Value;
        
        if (int.TryParse(weekStr, out int week))
            return week;

        // fallback if missing
        return 1;
    }

    public async Task<List<TeamWeeklyStats>> GetWeeklyTeamStatsAsync(string leagueKey)
    {
        var results = new List<TeamWeeklyStats>();

        // 1. Get all teams
        var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
        var teamsResponse = await _client.GetAsync(teamsUrl);
        var teamsXml = await teamsResponse.Content.ReadAsStringAsync();

        var teamsDoc = XDocument.Parse(teamsXml);
        XNamespace ns = teamsDoc.Root.GetDefaultNamespace();

        var teams = teamsDoc.Descendants(ns + "team")
            .Select(t => new {
                TeamKey = t.Element(ns + "team_key")?.Value,
                ManagerName = t.Descendants(ns + "manager")
                            .Elements(ns + "nickname")
                            .FirstOrDefault()?.Value
                            ?? "Unknown Manager"
            })
            .ToList();

        // get current week
        var effectiveDate = DateTime.UtcNow.Date.AddDays(-1);
        var week = await GetWeekForDateAsync(leagueKey, effectiveDate);

        // 2. Fetch weekly stats for each team
        foreach (var team in teams)
        {
            var statsUrl =
                $"https://fantasysports.yahooapis.com/fantasy/v2/team/{team.TeamKey}/stats;type=week;week={week}";

            var statsResponse = await _client.GetAsync(statsUrl);
            var statsXml = await statsResponse.Content.ReadAsStringAsync();
            var statsDoc = XDocument.Parse(statsXml);

            var statElems = statsDoc.Descendants(ns + "stat")
                .Select(s => new {
                    StatId = s.Element(ns + "stat_id")?.Value,
                    Value = s.Element(ns + "value")?.Value
                })
                .Where(s => s.StatId != null)
                .ToList();

            var statDict = statElems.ToDictionary(
                s => Helpers.GetStatDisplayName(s.StatId!),
                s => s.Value ?? "0"
            );

            results.Add(new TeamWeeklyStats
            {
                TeamKey = Helpers.Hash(team.TeamKey),
                ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
                StatValues = statDict
            });
        }

        return results;
    }

    public async Task<WeeklyLeagueSnapshot> GetWeeklyTeamResultsAsync(string leagueKey, string outputDirectory, bool DEBUG_STOP_AFTER_FIRST_TEAM = false)
    {
        var snapshot = new WeeklyLeagueSnapshot
        {
            Season = await GetSeasonAsync(leagueKey)
        };

        // Last completed fantasy week
        var effectiveDate = DateTime.UtcNow.Date.AddDays(-1);
        snapshot.Week = await GetWeekForDateAsync(leagueKey, effectiveDate);

        var (weekStart, weekEnd) = await GetWeekDateRangeAsync(leagueKey, snapshot.Week);
        snapshot.WeekStart = weekStart;
        snapshot.WeekEnd = weekEnd;

        // Write league context to correct folder
        await WriteLeagueContextAsync(snapshot.Season, snapshot.Week, outputDirectory);

        // Fetch teams
        var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
        var teamsXml = await _client.GetStringAsync(teamsUrl);
        var teamsDoc = XDocument.Parse(teamsXml);
        XNamespace teamsNs = teamsDoc.Root.GetDefaultNamespace();

        var teams = teamsDoc.Descendants(teamsNs + "team")
            .Select(t => new
            {
                TeamKey = t.Element(teamsNs + "team_key")?.Value,
                ManagerName = t.Descendants(teamsNs + "manager")
                            .FirstOrDefault()?.Element(teamsNs + "nickname")?.Value ?? "Unknown"
            })
            .Where(t => !string.IsNullOrEmpty(t.TeamKey))
            .ToList();

        Console.WriteLine($"Found {teams.Count} teams");

        foreach (var team in teams)
        {
            Console.WriteLine($"\nFetching WEEK {snapshot.Week} stats for {team.ManagerName}");

            var teamResult = new WeeklyTeamResult
            {
                TeamKey = Helpers.Hash(team.TeamKey),
                ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
                Week = snapshot.Week,
                Players = new List<WeeklyPlayerStats>()
            };

            var statsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/team/{team.TeamKey}/players/stats;type=week;week={snapshot.Week}";
            var statsXml = await _client.GetStringAsync(statsUrl);
            var statsDoc = XDocument.Parse(statsXml);
            XNamespace ns = statsDoc.Root.GetDefaultNamespace();

            foreach (var p in statsDoc.Descendants(ns + "player"))
            {
                var player = new WeeklyPlayerStats
                {
                    PlayerKey = Helpers.Hash(p.Element(ns + "player_key")?.Value),
                    FullName = p.Element(ns + "name")?.Element(ns + "full")?.Value,
                    Position = p.Element(ns + "display_position")?.Value,
                    NbaTeam = p.Element(ns + "editorial_team_abbr")?.Value,
                    RawStats = new Dictionary<string, double>()
                };

                foreach (var s in p.Descendants(ns + "stat"))
                {
                    var rawStatId = s.Element(ns + "stat_id")?.Value;
                    var statKey = Helpers.GetStatDisplayName(rawStatId);

                    if (!string.IsNullOrEmpty(statKey) &&
                        double.TryParse(s.Element(ns + "value")?.Value, out double val))
                    {
                        player.RawStats[statKey] = val;
                    }
                }


                player.FantasyPoints = Helpers.ComputeFantasyPoints(player.RawStats);
                teamResult.Players.Add(player);
            }

            snapshot.Teams.Add(teamResult);

            if (DEBUG_STOP_AFTER_FIRST_TEAM)
                break;
        }

        return snapshot;
    }
        
        public async Task DumpAllTeamRostersToJsonAsync(string leagueKey, string outputPath)
    {
        // 1. Get all teams and build teamKey → teamName/manager map
        var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
        var teamsResponse = await _client.GetAsync(teamsUrl);
        var teamsXml = await teamsResponse.Content.ReadAsStringAsync();
        var teamsDoc = XDocument.Parse(teamsXml);
        XNamespace ns = teamsDoc.Root.GetDefaultNamespace();

        var teams = teamsDoc.Descendants(ns + "team")
            .Select(t => new {
                TeamKey = t.Element(ns + "team_key")?.Value,
                ManagerName = t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value
            })
            .ToList();

        var allRosters = new List<TeamRoster>();

        // 2. For each team, fetch roster
        foreach (var team in teams)
        {
            var rosterUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/team/{team.TeamKey}/roster";
            var rosterResponse = await _client.GetAsync(rosterUrl);
            var rosterXml = await rosterResponse.Content.ReadAsStringAsync();
            var rosterDoc = XDocument.Parse(rosterXml);

            var players = rosterDoc.Descendants(ns + "player")
                .Select(p => new Player
                {
                    PlayerKey = Helpers.Hash(p.Element(ns + "player_key")?.Value),
                    FullName = p.Element(ns + "name")?.Element(ns + "full")?.Value,
                    Position = p.Element(ns + "display_position")?.Value,
                    NbaTeam = p.Element(ns + "editorial_team_abbr")?.Value
                })
                .ToList();

            allRosters.Add(new TeamRoster
            {
                TeamKey = Helpers.Hash(team.TeamKey),
                ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
                Players = players
            });
        }

        // 3. Serialize to JSON and save
        var json = JsonSerializer.Serialize(allRosters, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"All team rosters saved to {outputPath}");
    }

    public async Task DumpDraftResultsToJsonAsync(string leagueKey, string outputPath)
    {
        // 1. Get all teams and build teamKey → teamName map
        var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
        var teamsResponse = await _client.GetAsync(teamsUrl);
        var teamsXml = await teamsResponse.Content.ReadAsStringAsync();
        var teamsDoc = XDocument.Parse(teamsXml);
        XNamespace ns = teamsDoc.Root.GetDefaultNamespace();

        var teamMap = teamsDoc.Descendants(ns + "team")
            .ToDictionary(
                t => t.Element(ns + "team_key")?.Value,
                t => new {
                    ManagerName = t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value
                }
            );

        // 2. Get all draft results
        var draftUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/draftresults";
        var draftResponse = await _client.GetAsync(draftUrl);
        var draftXml = await draftResponse.Content.ReadAsStringAsync();
        var draftDoc = XDocument.Parse(draftXml);

        var draftResults = draftDoc.Descendants(ns + "draft_result")
            .Select(dr => new
            {
                round = int.Parse(dr.Element(ns + "round")?.Value ?? "0"),
                pick = int.Parse(dr.Element(ns + "pick")?.Value ?? "0"),
                team_key = dr.Element(ns + "team_key")?.Value,
                player_key = dr.Element(ns + "player_key")?.Value
            })
            .ToList();

        // 3. Optionally fetch player names and positions
        var resultsWithNames = new List<object>();
        foreach (var dr in draftResults)
        {
            string playerName = null;
            string position = null;

            // Fetch player info
            if (!string.IsNullOrEmpty(dr.player_key))
            {
                var playerUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/player/{dr.player_key}";
                var playerResponse = await _client.GetAsync(playerUrl);
                var playerXml = await playerResponse.Content.ReadAsStringAsync();
                var playerDoc = XDocument.Parse(playerXml);

                var playerElem = playerDoc.Descendants(ns + "player").FirstOrDefault();
                playerName = playerElem?.Element(ns + "name")?.Element(ns + "full")?.Value;
                position = playerElem?.Element(ns + "display_position")?.Value;
            }

            resultsWithNames.Add(new
            {
                dr.round,
                dr.pick,
                TeamKey = Helpers.Hash(dr.team_key),
                manager_name = Helpers.GetDisplayManagerName(teamMap.ContainsKey(dr.team_key) ? teamMap[dr.team_key].ManagerName : null),
                PlayerKey = Helpers.Hash(dr.player_key),
                player_name = playerName,
                position
            });
        }

        // 4. Serialize to JSON and save
        var json = JsonSerializer.Serialize(resultsWithNames, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        Console.WriteLine($"Draft results saved to {outputPath}");
    }
}