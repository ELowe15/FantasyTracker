using System.Text.Json;
using YahooApiConnector.Services;

public sealed class FantasyDataPipeline
{
    private readonly YahooFantasyService _fantasyService;
    private readonly string _leagueKey;
    private readonly string _dataPath;
    private readonly BestBallOrchestrator _bestBallOrchestrator;

    public FantasyDataPipeline(YahooFantasyService fantasyService, string leagueKey, string dataPath)
    {
        _fantasyService = fantasyService;
        _leagueKey = leagueKey;
        _dataPath = dataPath;
        _bestBallOrchestrator = new BestBallOrchestrator();
    }

    public async Task<int> RunAsync()
    {
        await ExportLeagueSnapshotsAsync();

        var snapshot = await _fantasyService.GetWeeklyTeamResultsAsync(_leagueKey, _dataPath);

        // Ensure season folder exists
        var seasonFolder = Path.Combine(_dataPath, snapshot.Season.ToString());
        if (!Directory.Exists(seasonFolder))
            Directory.CreateDirectory(seasonFolder);

        // Persist the raw weekly snapshot into the per-season folder (keeps existing structure)
        await PersistBestBallSnapshotAsync(snapshot, seasonFolder);

        // Delegate best-ball orchestration to the orchestrator (handles caching and rebuilds)
        await _bestBallOrchestrator.ProcessWeeklyAllModesAsync(snapshot, _dataPath);

        var weeklyStats = await _fantasyService.GetWeeklyTeamStatsAsync(_leagueKey);
        var roundRobinResults = RoundRobinService.RunRoundRobin(weeklyStats);

        var weeklySnapshot = new WeeklyStatsSnapshot
        {
            Season = snapshot.Season,
            Week = snapshot.Week,
            RoundRobinResults = roundRobinResults
        };

        var weeklyStatsOutPath = Path.Combine(_dataPath, $"round_robin_{snapshot.Season}_week_{snapshot.Week}.json");
        await File.WriteAllTextAsync(weeklyStatsOutPath, JsonSerializer.Serialize(weeklySnapshot, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Weekly stats snapshot saved to {weeklyStatsOutPath}");

        await RoundRobinService.RebuildSeasonRoundRobinAsync(snapshot.Season, _dataPath);
        await _fantasyService.WriteLeagueContextAsync(snapshot.Season, snapshot.Week, _dataPath);

        Console.WriteLine($"Season Round Robin snapshot rebuilt for season {snapshot.Season}");
        Console.WriteLine("Done.");
        return 0;
    }

    private async Task ExportLeagueSnapshotsAsync()
    {
        var rosterOutPath = Path.Combine(_dataPath, "team_results.json");
        await _fantasyService.DumpAllTeamRostersToJsonAsync(_leagueKey, rosterOutPath);

        var seasonStatsOutPath = Path.Combine(_dataPath, "season_stats.json");
        await _fantasyService.DumpAllTeamSeasonStatsToJsonAsync(_leagueKey, seasonStatsOutPath);

        Console.WriteLine($"Wrote team rosters to {rosterOutPath}");
    }

    private async Task PersistBestBallSnapshotAsync(WeeklyLeagueSnapshot snapshot, string seasonFolder)
    {
        var bestBallFileName = $"best_ball_{snapshot.Season}_week_{snapshot.Week}.json";
        var bestBallOutPath = Path.Combine(seasonFolder, bestBallFileName);

        var bestBallJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(bestBallOutPath, bestBallJson);

        Console.WriteLine($"Best Ball weekly results saved to {bestBallOutPath}");
        Console.WriteLine($"Season Best Ball snapshot persisted for season {snapshot.Season}");
    }
}
