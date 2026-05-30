using NbaSchedule.Models;
using NbaSchedule.Helpers;

namespace NbaSchedule.Services;

public class NbaScheduleService
{
    private readonly NbaStatsService _nbaService;
    private readonly SchedulePersistenceService _persistence;

    public NbaScheduleService(
        NbaStatsService nbaService,
        SchedulePersistenceService persistence)
    {
        _nbaService = nbaService;
        _persistence = persistence;
    }

    public async Task<List<NbaGame>> FetchAndSaveSeasonScheduleAsync(int season)
    {
        Console.WriteLine($"[NbaScheduleService] Fetching schedule for season {season}...");
        
        var games = await _nbaService.FetchSeasonGamesAsync(season);
        
        if (games.Count > 0)
        {
            await _persistence.SaveSeasonScheduleAsync(games, season);
            Console.WriteLine($"[NbaScheduleService] Saved {games.Count} games to persistent storage");

            var weekSchedules = WeekOrganizer.OrganizeGamesByWeek(games, season);
            await _persistence.SaveWeeklyScheduleAsync(weekSchedules, season);
            Console.WriteLine($"[NbaScheduleService] Organized into {weekSchedules.Count} weeks");
        }
        
        return games;
    }

    public async Task<NbaTeamSchedule> GetTeamScheduleAsync(
        string nbaTeamAbbreviation,
        int season)
    {
        var games = await _persistence.LoadSeasonScheduleAsync(season);
        
        var teamGames = games.Where(g =>
            g.HomeTeam == nbaTeamAbbreviation ||
            g.AwayTeam == nbaTeamAbbreviation
        ).ToList();

        return new NbaTeamSchedule
        {
            TeamAbbreviation = nbaTeamAbbreviation,
            TeamName = nbaTeamAbbreviation,
            Season = season,
            Games = teamGames
        };
    }

    public async Task<List<NbaGame>> GetTeamGamesForDateAsync(
        string nbaTeamAbbreviation,
        DateTime date,
        int season)
    {
        var games = await _persistence.LoadSeasonScheduleAsync(season);
        
        return games.Where(g =>
            (g.HomeTeam == nbaTeamAbbreviation || g.AwayTeam == nbaTeamAbbreviation) &&
            g.GameTime.Date == date.Date
        ).ToList();
    }

    public async Task<NbaGameDay> GetGamesForDateAsync(DateTime date, int season)
    {
        var games = await _persistence.LoadSeasonScheduleAsync(season);
        
        var dateGames = games.Where(g => g.GameTime.Date == date.Date).ToList();

        return new NbaGameDay
        {
            Date = date,
            Games = dateGames
        };
    }

    public bool ScheduleExistsForSeason(int season)
    {
        var scheduleDir = _persistence.GetScheduleDirectory(season);
        var filePath = Path.Combine(scheduleDir, "season_schedule.json");
        return File.Exists(filePath);
    }

    public List<int> GetAvailableSeasons()
    {
        var nbaDir = Path.Combine(Path.GetDirectoryName(_persistence.GetScheduleDirectory(2025)) ?? "", "..");
        
        if (!Directory.Exists(nbaDir))
            return new List<int>();

        var seasonDirs = Directory.GetDirectories(nbaDir)
            .Select(d => Path.GetFileName(d))
            .Where(name => int.TryParse(name, out _))
            .Select(int.Parse)
            .OrderByDescending(s => s)
            .ToList();

        return seasonDirs;
    }

    public async Task<NbaWeekSchedule> GetWeekScheduleAsync(int season, int weekNumber)
    {
        return await _persistence.LoadWeekScheduleAsync(season, weekNumber);
    }

    public async Task<List<NbaWeekSchedule>> GetAllWeeksAsync(int season)
    {
        return await _persistence.LoadAllWeeksAsync(season);
    }

    public async Task<List<NbaGame>> GetWeekGamesForTeamAsync(int season, int weekNumber, string teamAbbreviation)
    {
        var weekSchedule = await _persistence.LoadWeekScheduleAsync(season, weekNumber);
        return weekSchedule.Games
            .Where(g => g.HomeTeam == teamAbbreviation || g.AwayTeam == teamAbbreviation)
            .ToList();
    }
}
