using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var authService = new YahooAuthService(configuration);

        // Step 1: Get Authorization URL
        var authUrl = authService.GetAuthorizationUrl();
        Console.WriteLine("Go to the following URL to authorize:");
        Console.WriteLine(authUrl);

        Console.WriteLine("Enter the code from Yahoo:");
        var code = Console.ReadLine();

        // Step 2: Get Access Token
        var accessToken = await authService.GetAccessTokenAsync(code);

        // Step 3: Get League Key
        Console.WriteLine("Enter your Yahoo Fantasy League Key (e.g., 398.l.12345):");
        //var leagueKey = Console.ReadLine();
        var leagueKey = configuration["YahooApi:LeagueKey"];

        // Step 4: Fetch League Data
        var fantasyService = new YahooFantasyService(accessToken);

        await fantasyService.DumpDraftResultsToJsonAsync(leagueKey, "draft_results.json");
        
        Console.WriteLine("Yahoo Fantasy API REPL. Type 'exit' to quit.");
        while (true)
        {
            Console.WriteLine("\nEnter Yahoo Fantasy endpoint (e.g., league/{leagueKey}/teams):");
            var endpoint = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Trim().ToLower() == "exit")
                break;

            // Replace {leagueKey} with actual key if present
            endpoint = endpoint.Replace("{leagueKey}", leagueKey);

            try
            {
                var xml = await fantasyService.GetRawXmlAsync(leagueKey, endpoint, accessToken);
                Console.WriteLine("Raw XML response:");
                Console.WriteLine(xml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        Console.WriteLine("Exiting Yahoo Fantasy API REPL.");
    }
}
/*
        // List all your league keys
        //await fantasyService.ListUserLeaguesAsync();

        //var leagueData = await fantasyService.GetLeagueDataAsync(leagueKey);
        //Console.WriteLine("");
        //await fantasyService.GetLeagueTeamsAsync(leagueKey);
        //Console.WriteLine("");
        //await fantasyService.GetTeamRosterAsync(leagueKey);
        //Console.WriteLine("");
        //await fantasyService.GetDraftResultsAsync(leagueKey);
        //await fantasyService.GetDraftedPlayersAsJsonAsync(leagueKey);

        if (leagueData != null)
        {
            var outputPath = Path.Combine("data", $"league_{leagueKey}.json");
            Directory.CreateDirectory("data");
            await fantasyService.SaveLeagueDataAsJsonAsync(leagueData, outputPath);
            Console.WriteLine($"League data saved to {outputPath}");
        }
        else
        {
            Console.WriteLine("Failed to fetch or parse league data.");
        }
    }
}*/