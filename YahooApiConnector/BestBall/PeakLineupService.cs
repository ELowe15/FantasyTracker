public class PeakLineupService
{
    private readonly BestBallEngine _engine = new BestBallEngine();

    public void ProcessWeeklyPeak(WeeklyLeagueSnapshot snapshot, string dataPath)
    {
        if (snapshot == null || snapshot.Teams == null)
            return;

        var outSnapshot = new WeeklyLeagueSnapshot
        {
            Season = snapshot.Season,
            Week = snapshot.Week,
            WeekStart = snapshot.WeekStart,
            WeekEnd = snapshot.WeekEnd,
            Teams = new List<WeeklyTeamResult>()
        };

        BestBallSharedProcessor.ProcessWeekly(snapshot, dataPath, _engine, team => team.Players ?? new List<WeeklyPlayerStats>(), "peak_best_ball_{season}_week_{week}.json");
    }

    public async Task RebuildSeasonAsync(int season, string dataPath)
    {
        var seasonFolder = Path.Combine(dataPath, season.ToString());
        await BestBallAggregator.RebuildSeasonAsync(season, seasonFolder, $"peak_best_ball_{season}_week_*.json", $"season_peak_best_ball_{season}.json");
        Console.WriteLine($"Peak season best ball saved to {Path.Combine(seasonFolder, $"season_peak_best_ball_{season}.json")}");
    }
}
