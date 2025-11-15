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
        var outPath = Path.Combine(basePath, "team_results.json");

        try
        {
            await fantasyService.DumpAllTeamRostersToJsonAsync(leagueKey, outPath);
            Console.WriteLine($"Wrote results to {outPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to dump team rosters: " + ex.Message);
            return 5;
        }

        Console.WriteLine("Done.");
        return 0;
    }
}