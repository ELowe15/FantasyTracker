using NbaSchedule.Services;
using NbaSchedule.Models;

namespace NbaSchedule.Controllers;

public class ScheduleController
{
    private readonly NbaScheduleService _scheduleService;
    private readonly FantasyScheduleEnricherService _enrichedService;

    public ScheduleController(
        NbaScheduleService scheduleService,
        FantasyScheduleEnricherService enrichedService)
    {
        _scheduleService = scheduleService;
        _enrichedService = enrichedService;
    }

    public async Task<IEnumerable<NbaGame>> GetSeasonSchedule(int season)
    {
        if (!_scheduleService.ScheduleExistsForSeason(season))
            return new List<NbaGame>();

        var persistence = new SchedulePersistenceService(GetDataPath());
        return await persistence.LoadSeasonScheduleAsync(season);
    }

    public async Task<IEnumerable<NbaGame>> GetGamesInDateRange(int season, DateTime startDate, DateTime endDate)
    {
        var games = await GetSeasonSchedule(season);
        return games.Where(g => g.GameTime.Date >= startDate.Date && g.GameTime.Date <= endDate.Date).ToList();
    }

    public async Task<NbaTeamSchedule> GetTeamSchedule(string teamAbbr, int season)
    {
        return await _scheduleService.GetTeamScheduleAsync(teamAbbr, season);
    }

    public async Task<IEnumerable<NbaGame>> GetTeamGamesForDate(string teamAbbr, int season, DateTime date)
    {
        return await _scheduleService.GetTeamGamesForDateAsync(teamAbbr, date, season);
    }

    public async Task<NbaGameDay> GetGamesForDate(DateTime date, int season)
    {
        return await _scheduleService.GetGamesForDateAsync(date, season);
    }

    public async Task<List<PlayerGameInfo>> GetFantasyRosterWithSchedule(
        List<Player> fantasyRoster,
        DateTime date,
        int season)
    {
        return await _enrichedService.EnrichFantasyTeamForDateAsync(
            fantasyRoster,
            date,
            season
        );
    }

    public async Task<Dictionary<DateTime, List<PlayerGameInfo>>> GetFantasyRosterForWeek(
        List<Player> fantasyRoster,
        int season,
        DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(6);

        return await _enrichedService.EnrichFantasyTeamForWeekAsync(
            fantasyRoster,
            weekStart,
            weekEnd,
            season
        );
    }

    public async Task<RosterLoadSummary> GetRosterLoadAnalysis(
        List<Player> fantasyRoster,
        DateTime date,
        int season)
    {
        return await _enrichedService.GetRosterLoadForDateAsync(fantasyRoster, date, season);
    }

    public async Task<WeeklyRosterLoadSummary> GetWeeklyRosterLoadAnalysis(
        List<Player> fantasyRoster,
        int season,
        DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(6);

        return await _enrichedService.GetRosterLoadForWeekAsync(
            fantasyRoster,
            weekStart,
            weekEnd,
            season
        );
    }

    public List<int> GetAvailableSeasons()
    {
        return _scheduleService.GetAvailableSeasons();
    }

    private string GetDataPath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "Data");
    }
}
