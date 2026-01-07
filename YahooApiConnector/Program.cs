using System.Reflection;
using Microsoft.Extensions.Configuration;

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

        var fantasyService = new YahooFantasyService(accessToken);
        var bestBallService = new BestBallService();

        try
        {   
            var RosterOutPath = Path.Combine(basePath, "team_results.json");
            // 1. Dump rosters
            await fantasyService.DumpAllTeamRostersToJsonAsync(leagueKey, RosterOutPath);
            Console.WriteLine($"Wrote team rosters to {RosterOutPath}");

            /* 2. Dump weekly stats
            var statsOutPath = Path.Combine(basePath, "weekly_stats.json");
            await fantasyService.DumpWeeklyStatsToJsonAsync(leagueKey, statsOutPath);
            Console.WriteLine($"Wrote weekly stats to {statsOutPath}");*/
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            return 5;
        }

        try
        {
            var snapshot = await fantasyService.GetWeeklyTeamResultsAsync(leagueKey, basePath);

            // Run Best Ball using the teams inside the snapshot
            bestBallService.ProcessWeeklyBestBall(snapshot.Teams);

            // Build filename using SEASON + WEEK
            var fileName = $"best_ball_{snapshot.Season}_week_{snapshot.Week}.json";
            var outPath = Path.Combine(basePath, fileName);

            // Serialize the ENTIRE snapshot (not just teams)
            var json = System.Text.Json.JsonSerializer.Serialize(
                snapshot,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(outPath, json);

            Console.WriteLine(
                $"Best Ball weekly results saved to {outPath}");

            await bestBallService.RebuildSeasonBestBallAsync(snapshot.Season, basePath);

            Console.WriteLine(
                $"Season Best Ball snapshot rebuilt for season {snapshot.Season}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error processing Best Ball: " + ex.Message);
            return 5;
        }

        Console.WriteLine("Done.");
        return 0;
    }
}