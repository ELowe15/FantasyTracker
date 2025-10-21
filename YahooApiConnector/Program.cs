using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var autoMode = args.Contains("--auto");

        var authService = new YahooAuthService(configuration);

        string accessToken;

        if (autoMode)
        {
            // Use refresh token from environment
            var refreshToken = Environment.GetEnvironmentVariable("YAHOO_REFRESH_TOKEN");
            if (string.IsNullOrEmpty(refreshToken))
                throw new Exception("Missing YAHOO_REFRESH_TOKEN in environment variables.");

            accessToken = await authService.GetAccessTokenFromRefreshTokenAsync(refreshToken);
        }
        else
        {
            // Manual OAuth flow
            var authUrl = authService.GetAuthorizationUrl();
            Console.WriteLine("Go to the following URL to authorize:");
            Console.WriteLine(authUrl);
            Console.WriteLine("Enter the code from Yahoo:");
            var code = Console.ReadLine();

            var tokens = await authService.GetAccessTokenAsync(code);
            accessToken = tokens.accessToken;
            var refreshToken = tokens.refreshToken;

            Console.WriteLine($"Access token: {accessToken}");
            Console.WriteLine($"REFRESH token (save this!): {refreshToken}");
        }

        var leagueKey = configuration["YahooApi:LeagueKey"];
        var fantasyService = new YahooFantasyService(accessToken);

        await fantasyService.DumpAllTeamRostersToJsonAsync(leagueKey, "team_results.json");
        Console.WriteLine("Done.");
    }
}
