using Microsoft.Extensions.Configuration;
using NbaSchedule.Services;

public class FetchNbaScheduleCommand
{
    private readonly IConfiguration _configuration;
    private readonly string _dataPath;

    public FetchNbaScheduleCommand(IConfiguration configuration, string dataPath)
    {
        _configuration = configuration;
        _dataPath = dataPath;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        int season = 2025;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedSeason))
            season = parsedSeason;

        try
        {
            var nbaService = new NbaStatsService();
            var persistence = new SchedulePersistenceService(_dataPath);
            var scheduleService = new NbaScheduleService(nbaService, persistence);

            var games = await scheduleService.FetchAndSaveSeasonScheduleAsync(season);

            Console.WriteLine($"✓ Successfully fetched and saved {games.Count} games for season {season}");
            Console.WriteLine($"✓ Data saved to: {Path.Combine(_dataPath, "nba", season.ToString(), "schedule")}");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
