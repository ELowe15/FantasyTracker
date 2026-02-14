using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Prefer the current working directory (same behavior as before),
        // fall back to the executable base dir if needed.
        var primaryBase = Directory.GetCurrentDirectory();
        var fallbackBase = AppContext.BaseDirectory;

        // Choose base path where appsettings.json exists
        var basePath = primaryBase;
        if (!File.Exists(Path.Combine(primaryBase, "appsettings.json")) &&
            File.Exists(Path.Combine(fallbackBase, "appsettings.json")))
        {
            basePath = fallbackBase;
        }

        var dataPath = Path.Combine(basePath, "Data");

        Console.WriteLine($"Config base path: {basePath}");
        Console.WriteLine($"CWD: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"ExeDir: {AppContext.BaseDirectory}");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var authService = new YahooAuthService(configuration);

        string clientId = configuration["YahooApi:ClientId"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_ID");
        string clientSecret = configuration["YahooApi:ClientSecret"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_SECRET");
        string refreshToken = configuration["YahooApi:RefreshToken"] ?? Environment.GetEnvironmentVariable("YAHOO_REFRESH_TOKEN");
        string leagueKey = configuration["YahooApi:LeagueKey"] ?? Environment.GetEnvironmentVariable("YAHOO_LEAGUE_KEY");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            Console.Error.WriteLine("Missing Yahoo client id/secret. Provide via appsettings or YAHOO_CLIENT_ID/YAHOO_CLIENT_SECRET env vars.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            Console.Error.WriteLine("Missing refresh token. Provide via appsettings or YAHOO_REFRESH_TOKEN env var.");
            return 3;
        }

        authService = new YahooAuthService(configuration);

        string accessToken;
        
        try
        {
            accessToken = await authService.GetAccessTokenFromRefreshTokenAsync(refreshToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to obtain access token: " + ex.Message);
            return 4;
        }
        var ImageOutPath = Path.Combine(dataPath, "player_images.json");
        var fantasyService = new YahooFantasyService(accessToken, ImageOutPath);
        var bestBallService = new BestBallService();
        //await fantasyService.GetFirstTeamAllPlayerStatsForDateAsync(leagueKey, DateTime.UtcNow.Date.AddDays(-1));

        try
        {   
            var RosterOutPath = Path.Combine(dataPath, "team_results.json");
            // 1. Dump rosters
            await fantasyService.DumpAllTeamRostersToJsonAsync(leagueKey, RosterOutPath);

            var SeasonStatsOutPath = Path.Combine(dataPath, "season_stats.json");
            // 1. Dump rosters
            await fantasyService.DumpAllTeamSeasonStatsToJsonAsync(leagueKey, SeasonStatsOutPath);
            //var DraftOutPath = Path.Combine(dataPath, "draft_results.json");
            // 1. Dump draft results
            //await fantasyService.DumpDraftResultsToJsonAsync(leagueKey, DraftOutPath);
            Console.WriteLine($"Wrote team rosters to {RosterOutPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            return 5;
        }

        try
{

    var snapshot = await fantasyService.GetWeeklyTeamResultsAsync(
        leagueKey,
        dataPath
    );

    // Run Best Ball
    bestBallService.ProcessWeeklyBestBall(snapshot.Teams);

    var bestBallFileName =
        $"best_ball_{snapshot.Season}_week_{snapshot.Week}.json";

    var bestBallOutPath =
        Path.Combine(dataPath, bestBallFileName);

    var bestBallJson =
        JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions { WriteIndented = true }
        );

    await File.WriteAllTextAsync(bestBallOutPath, bestBallJson);

    Console.WriteLine(
        $"Best Ball weekly results saved to {bestBallOutPath}"
    );

    await bestBallService.RebuildSeasonBestBallAsync(
        snapshot.Season,
        dataPath
    );

    Console.WriteLine(
        $"Season Best Ball snapshot rebuilt for season {snapshot.Season}"
    );

    // -------------------------
    // WEEKLY TEAM STATS
    // -------------------------
    var weeklyStats =
        await fantasyService.GetWeeklyTeamStatsAsync(leagueKey);

    var roundRobinResults =
        RoundRobinService.RunRoundRobin(weeklyStats);

    // Build WEEKLY SNAPSHOT (explicit, clean)
    var weeklySnapshot = new WeeklyStatsSnapshot
    {
        Season = snapshot.Season,
        Week = snapshot.Week,
        RoundRobinResults = roundRobinResults
    };

    var weeklyStatsFileName =
        $"round_robin_{snapshot.Season}_week_{snapshot.Week}.json";

    var weeklyStatsOutPath =
        Path.Combine(dataPath, weeklyStatsFileName);

    var weeklyStatsJson =
        JsonSerializer.Serialize(
            weeklySnapshot,
            new JsonSerializerOptions { WriteIndented = true }
        );

    await File.WriteAllTextAsync(weeklyStatsOutPath, weeklyStatsJson);

    Console.WriteLine(
        $"Weekly stats snapshot saved to {weeklyStatsOutPath}"
    );

    await RoundRobinService.RebuildSeasonRoundRobinAsync(
        snapshot.Season,
        dataPath
    );

    Console.WriteLine(
        $"Season Round Robin snapshot rebuilt for season {snapshot.Season}"
    );
}
catch (Exception ex)
{
    Console.Error.WriteLine(
        "Error processing weekly fantasy pipeline: " + ex
    );
    return 5;
}


        Console.WriteLine("Done.");
        return 0;
    }
}