using System.Text.Json;
using YahooApiConnector.Services.Interfaces;

namespace YahooApiConnector.Services;
public class YahooFantasyService
{
    private readonly YahooFantasyApiClient _apiClient;
    private readonly IPlayerImageService _imageService;
    private readonly IRosterService _rosterService;
    private readonly IStatsService _statsService;
    private readonly ISnapshotService _snapshotService;
    private int dayOffset = -150;

    public YahooFantasyService(YahooFantasyApiClient apiClient,
        IPlayerImageService imageService,
        IRosterService rosterService,
        IStatsService statsService,
        ISnapshotService snapshotService)
    {
        _apiClient = apiClient;
        _imageService = imageService;
        _rosterService = rosterService;
        _statsService = statsService;
        _snapshotService = snapshotService;
    }

    public async Task<(DateTime start, DateTime end)> GetWeekDateRangeAsync(
        string leagueKey,
        int week)
    {
        var doc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.LeagueScoreboard(leagueKey, week));
        return YahooFantasyXmlParser.GetWeekDateRange(doc);
    }

    public async Task WriteLeagueContextAsync(int season, int currentWeek, string outputDirectory)
    {
        outputDirectory = YahooFantasyPersistence.EnsureDirectoryExists(outputDirectory);

        var seasonFolder = Path.Combine(outputDirectory, season.ToString());
        YahooFantasyPersistence.EnsureDirectoryExists(seasonFolder);

        var availableWeeks = _snapshotService.GetAvailableWeeks(season, seasonFolder);

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

        var outputPath = Path.Combine(seasonFolder, "league_context.json");
        await YahooFantasyPersistence.WriteJsonFileAsync(outputPath, context);
        await YahooFantasyPersistence.WriteJsonFileAsync(
            Path.Combine(outputDirectory, "league_context.json"),
            context);
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
        var doc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.League(leagueKey));
        return YahooFantasyXmlParser.GetCurrentWeek(doc, 1);
    }

    public async Task<List<TeamWeeklyStats>> GetWeeklyTeamStatsAsync(string leagueKey)
    {
        var teams = await _apiClient.GetLeagueTeamsAsync(leagueKey);
        var effectiveDate = DateTime.UtcNow.Date.AddDays(dayOffset);
        var week = await GetWeekForDateAsync(leagueKey, effectiveDate);

        var results = new List<TeamWeeklyStats>();
        foreach (var team in teams)
        {
            results.Add(await BuildTeamWeeklyStatsAsync(team, week));
        }

        return results;
    }

    public async Task<WeeklyLeagueSnapshot> GetWeeklyTeamResultsAsync(
        string leagueKey,
        string outputDirectory,
        bool DEBUG_STOP_AFTER_FIRST_TEAM = false)
    {
        var effectiveDate = DateTime.UtcNow.Date.AddDays(dayOffset); // yesterday
        var season = await GetSeasonAsync(leagueKey);
        var week = await GetWeekForDateAsync(leagueKey, effectiveDate);
        var (weekStart, weekEnd) = await GetWeekDateRangeAsync(leagueKey, week);

        var bestBallFileName = $"best_ball_{season}_week_{week}.json";

        // Prefer the categorized season folder, while retaining support for earlier layouts.
        var seasonFolder = Path.Combine(outputDirectory, season.ToString());
        YahooFantasyPersistence.EnsureDirectoryExists(seasonFolder);
        var bestBallFolder = Path.Combine(seasonFolder, "BestBall");
        YahooFantasyPersistence.EnsureDirectoryExists(bestBallFolder);

        var candidatePaths = new[] {
            Path.Combine(bestBallFolder, bestBallFileName),
            Path.Combine(outputDirectory, bestBallFileName),
            Path.Combine(seasonFolder, bestBallFileName)
        };

        WeeklyLeagueSnapshot snapshot = null!;
        var existingPath = candidatePaths.FirstOrDefault(File.Exists);
        if (!string.IsNullOrWhiteSpace(existingPath))
        {
            var json = await File.ReadAllTextAsync(existingPath);
            snapshot = JsonSerializer.Deserialize<WeeklyLeagueSnapshot>(json)
                    ?? await _snapshotService.InitializeNewWeeklySnapshot(leagueKey, season, week, weekStart, weekEnd);
        }
        else
        {
            snapshot = await _snapshotService.InitializeNewWeeklySnapshot(leagueKey, season, week, weekStart, weekEnd);
        }

        snapshot.ProcessedDates ??= new HashSet<DateTime>();
        if (snapshot.ProcessedDates.Contains(effectiveDate))
        {
            Console.WriteLine("[YahooFantasyService] Effective date already processed in snapshot; returning existing snapshot.");
            return snapshot;
        }

        Dictionary<string, List<WeeklyPlayerStats>> dailyResults;
        try
        {
            dailyResults = await _statsService.GetDailyLeagueResultsAsync(leagueKey, effectiveDate, DEBUG_STOP_AFTER_FIRST_TEAM);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[YahooFantasyService] Failed to get daily league results: {ex.Message}");
            throw;
        }

        _snapshotService.MergeDailyIntoWeekly(snapshot, dailyResults);

        snapshot.ProcessedDates.Add(effectiveDate);

        var writePath = Path.Combine(bestBallFolder, bestBallFileName);
        await _snapshotService.WriteWeeklySnapshotAsync(writePath, snapshot);

        return snapshot;
    }

    public async Task<List<WeeklyPlayerStats>> GetFirstTeamAllPlayerStatsForDateAsync(
        string leagueKey,
        DateTime date)
    {
        return await _statsService.GetFirstTeamAllPlayerStatsForDateAsync(leagueKey, date);
    }

    public async Task DumpAllTeamRostersToJsonAsync(string leagueKey, string outputPath)
    {
        // Delegate roster building + image persistence to the RosterService
        await _rosterService.DumpAllTeamRostersToJsonAsync(leagueKey, outputPath);
        Console.WriteLine($"All team rosters saved to {outputPath}");
    }

    public async Task DumpAllTeamSeasonStatsToJsonAsync(string leagueKey, string outputPath)
    {
        var teamsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.LeagueTeams(leagueKey));
        var ns = YahooFantasyXmlParser.GetNamespace(teamsDoc);

        var teams = teamsDoc.Descendants(ns + "team")
            .Select(t => new
            {
                TeamKey = t.Element(ns + "team_key")?.Value,
                ManagerName = t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value ?? "Unknown"
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.TeamKey))
            .ToList();

        var allTeamStats = new List<object>();
        foreach (var team in teams)
        {
            var statsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.TeamSeasonStats(team.TeamKey!));
            var statElems = statsDoc.Descendants(ns + "stat")
                .Select(s => new
                {
                    StatId = s.Element(ns + "stat_id")?.Value,
                    Value = s.Element(ns + "value")?.Value
                })
                .Where(s => !string.IsNullOrWhiteSpace(s.StatId))
                .ToList();

            var statDict = statElems.ToDictionary(
                s => Helpers.GetStatDisplayName(s.StatId!),
                s => s.Value ?? "0");

            allTeamStats.Add(new
            {
                TeamKey = Helpers.Hash(team.TeamKey!),
                ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
                StatValues = statDict
            });
        }

        await YahooFantasyPersistence.WriteJsonFileAsync(outputPath, allTeamStats);
        Console.WriteLine($"All team season stats saved to {outputPath}");
    }

    public async Task DumpDraftResultsToJsonAsync(string leagueKey, string outputPath)
    {
        var teamsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.LeagueTeams(leagueKey));
        var ns = YahooFantasyXmlParser.GetNamespace(teamsDoc);

        await _imageService.LoadAsync();
        var imagesModified = false;

        var teamMap = teamsDoc.Descendants(ns + "team")
            .Where(t => !string.IsNullOrWhiteSpace(t.Element(ns + "team_key")?.Value))
            .ToDictionary(
                t => t.Element(ns + "team_key")!.Value,
                t => new
                {
                    ManagerName = t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value
                });

        var draftDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.LeagueDraftResults(leagueKey));
        var draftResults = draftDoc.Descendants(ns + "draft_result")
            .Select(dr => new
            {
                Round = int.Parse(dr.Element(ns + "round")?.Value ?? "0"),
                Pick = int.Parse(dr.Element(ns + "pick")?.Value ?? "0"),
                TeamKey = dr.Element(ns + "team_key")?.Value,
                PlayerKey = dr.Element(ns + "player_key")?.Value
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.TeamKey) && !string.IsNullOrWhiteSpace(item.PlayerKey))
            .ToList();

        var resultsWithNames = new List<object>();
        foreach (var draftEntry in draftResults)
        {
            string? playerName = null;
            string? position = null;

            if (!string.IsNullOrWhiteSpace(draftEntry.PlayerKey))
            {
                var playerDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.PlayerDetails(draftEntry.PlayerKey));
                var playerElem = playerDoc.Descendants(ns + "player").FirstOrDefault();
                playerName = playerElem?.Element(ns + "name")?.Element(ns + "full")?.Value;
                position = playerElem?.Element(ns + "display_position")?.Value;
                var imageUrl = playerElem?.Element(ns + "image_url")?.Value;
                var hashedPlayerKey = Helpers.Hash(draftEntry.PlayerKey);

                if (_imageService.TryAdd(hashedPlayerKey, imageUrl))
                    imagesModified = true;
            }

            resultsWithNames.Add(new
            {
                round = draftEntry.Round,
                pick = draftEntry.Pick,
                TeamKey = Helpers.Hash(draftEntry.TeamKey!),
                manager_name = Helpers.GetDisplayManagerName(teamMap.TryGetValue(draftEntry.TeamKey!, out var teamInfo) ? teamInfo.ManagerName : null),
                PlayerKey = Helpers.Hash(draftEntry.PlayerKey!),
                player_name = playerName,
                position
            });
        }

        await YahooFantasyPersistence.WriteJsonFileAsync(outputPath, resultsWithNames);


        if (imagesModified)
            await _imageService.SaveAsync();

        Console.WriteLine($"Draft results saved to {outputPath}");
    }

    private async Task<TeamWeeklyStats> BuildTeamWeeklyStatsAsync(YahooTeamSummary team, int week)
    {
        var statsDoc = await _apiClient.GetDocumentAsync(YahooFantasyUrlBuilder.TeamStats(team.TeamKey, week));
        var ns = YahooFantasyXmlParser.GetNamespace(statsDoc);

        var statDict = statsDoc.Descendants(ns + "stat")
            .Where(s => s.Element(ns + "stat_id") != null)
            .ToDictionary(
                s => Helpers.GetStatDisplayName(s.Element(ns + "stat_id")?.Value ?? string.Empty),
                s => s.Element(ns + "value")?.Value ?? "0",
                StringComparer.OrdinalIgnoreCase);

        return new TeamWeeklyStats
        {
            TeamKey = Helpers.Hash(team.TeamKey),
            ManagerName = Helpers.GetDisplayManagerName(team.ManagerName),
            StatValues = statDict
        };
    }

    private async Task<int> GetSeasonAsync(string leagueKey)
    {
        var url = YahooFantasyUrlBuilder.LeagueSettings(leagueKey);
        var doc = await _apiClient.GetDocumentAsync(url);
        return YahooFantasyXmlParser.GetSeason(doc, DateTime.Now.Year);
    }
}