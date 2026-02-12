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

        if (!availableWeeks.Contains(currentWeek))
        {
            availableWeeks.Add(currentWeek);
            availableWeeks.Sort();
        }

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

public async Task<WeeklyLeagueSnapshot> GetWeeklyTeamResultsAsync(
    string leagueKey,
    string outputDirectory,
    bool DEBUG_STOP_AFTER_FIRST_TEAM = false)
{
    // -----------------------
    // 1. Determine week and season
    // -----------------------
    var effectiveDate = DateTime.UtcNow.Date.AddDays(-1); // yesterday
    var season = await GetSeasonAsync(leagueKey);
    var week = await GetWeekForDateAsync(leagueKey, effectiveDate);
    var (weekStart, weekEnd) = await GetWeekDateRangeAsync(leagueKey, week);

    // -----------------------
    // 2. Locate the weekly file (existing or new)
    // -----------------------
    var bestBallFileName = $"best_ball_{season}_week_{week}.json";
    var bestBallFilePath = Path.Combine(outputDirectory, bestBallFileName);

    WeeklyLeagueSnapshot snapshot;

    if (File.Exists(bestBallFilePath))
    {
        var json = await File.ReadAllTextAsync(bestBallFilePath);
        snapshot = JsonSerializer.Deserialize<WeeklyLeagueSnapshot>(json)
                   ?? await InitializeNewWeeklySnapshot(leagueKey, season, week, weekStart, weekEnd);
    }
    else
    {
        snapshot = await InitializeNewWeeklySnapshot(leagueKey, season, week, weekStart, weekEnd);
    }

    // -----------------------
    // 3. Skip if this date already processed
    // -----------------------
    snapshot.ProcessedDates ??= new HashSet<DateTime>();
    if (snapshot.ProcessedDates.Contains(effectiveDate))
    {
        return snapshot;
    }

    // -----------------------
    // 4. Fetch daily stats (single call per team, no per-player calls)
    // -----------------------
    var dailyResults = await GetDailyLeagueResultsAsync(leagueKey, effectiveDate, DEBUG_STOP_AFTER_FIRST_TEAM);

    // -----------------------
    // 5. Merge daily stats into weekly snapshot
    // -----------------------
    MergeDailyIntoWeekly(snapshot, dailyResults);

    // -----------------------
    // 6. Mark date processed
    // -----------------------
    snapshot.ProcessedDates.Add(effectiveDate);

    // -----------------------
    // 7. Save updated snapshot back to same file
    // -----------------------
    var updatedJson = JsonSerializer.Serialize(
        snapshot,
        new JsonSerializerOptions { WriteIndented = true });

    await File.WriteAllTextAsync(bestBallFilePath, updatedJson);

    return snapshot;
}

private async Task<WeeklyLeagueSnapshot> InitializeNewWeeklySnapshot(
    string leagueKey,
    int season,
    int week,
    DateTime weekStart,
    DateTime weekEnd)
{
    var snapshot = new WeeklyLeagueSnapshot
    {
        Season = season,
        Week = week,
        WeekStart = weekStart,
        WeekEnd = weekEnd,
        Teams = new List<WeeklyTeamResult>(),
        ProcessedDates = new HashSet<DateTime>()
    };

    // Fetch teams
    var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
    var teamsXml = await _client.GetStringAsync(teamsUrl);
    var teamsDoc = XDocument.Parse(teamsXml);
    XNamespace ns = teamsDoc.Root.GetDefaultNamespace();

    var teams = teamsDoc.Descendants(ns + "team")
        .Select(t => new WeeklyTeamResult
        {
            TeamKey = Helpers.Hash(t.Element(ns + "team_key")?.Value),
            ManagerName = Helpers.GetDisplayManagerName(
                t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value ?? "Unknown"),
            Week = week,
            Players = new List<WeeklyPlayerStats>()
        })
        .ToList();

    snapshot.Teams = teams;

    return snapshot;
}

private async Task<Dictionary<string, List<WeeklyPlayerStats>>> GetDailyLeagueResultsAsync(
    string leagueKey,
    DateTime date,
    bool DEBUG_STOP_AFTER_FIRST_TEAM)
{
    var result = new Dictionary<string, List<WeeklyPlayerStats>>();
    XNamespace ns;

    // -----------------------
    // 1. Fetch all teams
    // -----------------------
var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams/players";
    var teamsXml = await _client.GetStringAsync(teamsUrl);
    var teamsDoc = XDocument.Parse(teamsXml);
    ns = teamsDoc.Root.GetDefaultNamespace();

    var teams = teamsDoc.Descendants(ns + "team")
        .Select(t => new
        {
            TeamKey = t.Element(ns + "team_key")?.Value,
            Roster = t.Descendants(ns + "player")
                      .Select(p => p.Element(ns + "player_key")?.Value)
                      .Where(k => !string.IsNullOrEmpty(k))
                      .ToList()
        })
        .ToList();

    // -----------------------
    // 2. Collect all unique player keys across all teams
    // -----------------------
    var allPlayerKeys = teams.SelectMany(t => t.Roster).Distinct().ToList();

    // -----------------------
    // 3. Fetch stats in batches
    // -----------------------
    const int batchSize = 25; // adjust if needed
    var dailyStats = new Dictionary<string, WeeklyPlayerStats>();

    for (int i = 0; i < allPlayerKeys.Count; i += batchSize)
    {
        var batchKeys = allPlayerKeys.Skip(i).Take(batchSize);
        var keysCsv = string.Join(",", batchKeys);

        var statsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/players;player_keys={keysCsv}/stats;type=date;date={date:yyyy-MM-dd}";
        var statsXml = await _client.GetStringAsync(statsUrl);
        var statsDoc = XDocument.Parse(statsXml);
        ns = statsDoc.Root.GetDefaultNamespace();

        foreach (var p in statsDoc.Descendants(ns + "player"))
        {
            var playerKey = p.Element(ns + "player_key")?.Value;
            var statDict = new Dictionary<string, double>();

            foreach (var s in p.Descendants(ns + "stat"))
            {
                var statId = s.Element(ns + "stat_id")?.Value;
                var statKey = Helpers.GetStatDisplayName(statId);
                if (!string.IsNullOrEmpty(statKey) &&
                    double.TryParse(s.Element(ns + "value")?.Value, out var val))
                {
                    statDict[statKey] = val;
                }
            }

            // Filter to only the standard Best Ball stats
            statDict = Helpers.FilterToBestBallStats(statDict);

            if (statDict.Count == 0)
            {
                continue;
            }

            dailyStats[playerKey] = new WeeklyPlayerStats
            {
                PlayerKey = Helpers.Hash(playerKey),
                FullName = p.Element(ns + "name")?.Element(ns + "full")?.Value,
                Position = p.Element(ns + "display_position")?.Value,
                NbaTeam = p.Element(ns + "editorial_team_abbr")?.Value,
                RawStats = statDict,
                FantasyPoints = Helpers.ComputeFantasyPoints(statDict)
            };

        }

        if (DEBUG_STOP_AFTER_FIRST_TEAM)
            break;
    }

    // -----------------------
    // 4. Map stats to teams
    // -----------------------
    foreach (var team in teams)
    {
        var teamHashedKey = Helpers.Hash(team.TeamKey);
        var teamPlayerStats = new List<WeeklyPlayerStats>();

        foreach (var playerKey in team.Roster)
        {
            if (dailyStats.TryGetValue(playerKey, out var stats))
            {
                teamPlayerStats.Add(stats);
            }
            else
            {
            }
        }

        result[teamHashedKey] = teamPlayerStats;

        if (DEBUG_STOP_AFTER_FIRST_TEAM)
            break;
    }

    return result;
}


private void MergeDailyIntoWeekly(
    WeeklyLeagueSnapshot snapshot,
    Dictionary<string, List<WeeklyPlayerStats>> dailyResults)
{
    foreach (var team in snapshot.Teams)
    {
        if (!dailyResults.ContainsKey(team.TeamKey))
            continue;

        foreach (var dailyPlayer in dailyResults[team.TeamKey])
        {
            var existing = team.Players.FirstOrDefault(p => p.PlayerKey == dailyPlayer.PlayerKey);

            if (existing == null)
            {
                existing = new WeeklyPlayerStats
                {
                    PlayerKey = dailyPlayer.PlayerKey,
                    FullName = dailyPlayer.FullName,
                    Position = dailyPlayer.Position,
                    NbaTeam = dailyPlayer.NbaTeam,
                    RawStats = new Dictionary<string, double>()
                };
                team.Players.Add(existing);
            }

            // Only sum counting stats; recompute percentages later
            foreach (var stat in dailyPlayer.RawStats)
            {
                if (stat.Key == "FG%" || stat.Key == "FT%")
                    continue;

                if (!existing.RawStats.ContainsKey(stat.Key))
                    existing.RawStats[stat.Key] = 0;

                existing.RawStats[stat.Key] += stat.Value;
            }

            existing.FantasyPoints = Helpers.ComputeFantasyPoints(existing.RawStats);
        }
    }
}



public async Task<List<WeeklyPlayerStats>> GetFirstTeamAllPlayerStatsForDateAsync(
    string leagueKey,
    DateTime date)
{
    var results = new List<WeeklyPlayerStats>();

    // ---------------------------
    // CALL 1: Get first team + all players
    // ---------------------------

    var teamsUrl =
        $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams;out=players";

    var teamsXml = await _client.GetStringAsync(teamsUrl);
    var teamsDoc = XDocument.Parse(teamsXml);
    XNamespace ns = teamsDoc.Root.GetDefaultNamespace();

    var teams = teamsDoc.Descendants(ns + "team").ToList();

Console.WriteLine($"Found {teams.Count} teams");

if (teams.Count < 2)
{
    Console.WriteLine("Second team not found.");
    return results;
}

var secondTeam = teams[1];

var managerName = secondTeam
    .Descendants(ns + "manager")
    .FirstOrDefault()?
    .Element(ns + "nickname")?.Value ?? "Unknown";

Console.WriteLine($"Fetching players for manager: {managerName}");


    var playerKeys = secondTeam
        .Descendants(ns + "player")
        .Select(p => p.Element(ns + "player_key")?.Value)
        .Where(k => !string.IsNullOrEmpty(k))
        .ToList();

    var formattedDate = date.ToString("yyyy-MM-dd");

    // ---------------------------
    // CALL 2: Loop through players
    // ---------------------------

    foreach (var playerKey in playerKeys)
    {
        var statsUrl =
            $"https://fantasysports.yahooapis.com/fantasy/v2/player/{playerKey}/stats;type=date;date={formattedDate}";

        var statsXml = await _client.GetStringAsync(statsUrl);
        var statsDoc = XDocument.Parse(statsXml);

        var playerNode = statsDoc.Descendants(ns + "player").FirstOrDefault();
        if (playerNode == null)
            continue;

        var player = new WeeklyPlayerStats
        {
            PlayerKey = Helpers.Hash(playerKey),
            FullName = playerNode.Element(ns + "name")?.Element(ns + "full")?.Value,
            Position = playerNode.Element(ns + "display_position")?.Value,
            NbaTeam = playerNode.Element(ns + "editorial_team_abbr")?.Value,
            RawStats = new Dictionary<string, double>()
        };

        foreach (var s in playerNode.Descendants(ns + "stat"))
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
        results.Add(player);
    }

    return results;
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