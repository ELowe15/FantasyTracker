using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YahooApiConnector;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var primaryBase = Directory.GetCurrentDirectory();
        var fallbackBase = AppContext.BaseDirectory;

        var basePath = primaryBase;
        if (!File.Exists(Path.Combine(primaryBase, "appsettings.json")) &&
            File.Exists(Path.Combine(fallbackBase, "appsettings.json")))
        {
            basePath = fallbackBase;
        }

        var dataPath = Path.Combine(basePath, "Data");
        Directory.CreateDirectory(dataPath);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var authService = new YahooAuthService(configuration);

        string? clientId = configuration["YahooApi:ClientId"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_ID");
        string? clientSecret = configuration["YahooApi:ClientSecret"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_SECRET");
        string? refreshToken = configuration["YahooApi:RefreshToken"] ?? Environment.GetEnvironmentVariable("YAHOO_REFRESH_TOKEN");
        string? leagueKey = configuration["YahooApi:LeagueKey"] ?? Environment.GetEnvironmentVariable("YAHOO_LEAGUE_KEY");

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

        if (string.IsNullOrWhiteSpace(leagueKey))
        {
            Console.Error.WriteLine("Missing league key. Provide via appsettings or YAHOO_LEAGUE_KEY env var.");
            return 6;
        }

        string accessToken;

        try
        {
            //string authUrl = authService.GetAuthorizationUrl();
            //(string taccessToken, string trefreshToken) = await authService.GetAccessTokenAsync("");
            //Console.WriteLine($"[Program] Access token obtained:  {trefreshToken}");
            accessToken = await authService.GetAccessTokenFromRefreshTokenAsync(refreshToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to obtain access token: " + ex.Message);
            return 4;
        }

        var imageOutPath = Path.Combine(dataPath, "player_images.json");

        var services = new ServiceCollection();
        services.AddYahooServices(accessToken, imageOutPath);

        using var provider = services.BuildServiceProvider();

        try
        {
            var fantasyService = provider.GetRequiredService<YahooApiConnector.Services.YahooFantasyService>();
            var accountService = provider.GetRequiredService<YahooApiConnector.Services.YahooAccountService>();

            Console.WriteLine("Testing Yahoo connection by listing teams for the configured league...");
            try
            {
                await accountService.PrintUserTeamsXmlAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to list teams: " + ex.Message);
                // continue to pipeline to let pipeline show full failure if different
            }

            var pipeline = new FantasyDataPipeline(fantasyService, leagueKey, dataPath);
            return await pipeline.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error processing Yahoo fantasy pipeline: " + ex.Message);
            return 5;
        }
    }
}