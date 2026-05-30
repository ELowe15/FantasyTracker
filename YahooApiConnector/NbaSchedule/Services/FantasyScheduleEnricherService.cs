using NbaSchedule.Models;

namespace NbaSchedule.Services;

public class FantasyScheduleEnricherService
{
    private readonly NbaScheduleService _scheduleService;

    public FantasyScheduleEnricherService(NbaScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    public async Task<List<PlayerGameInfo>> EnrichFantasyTeamForDateAsync(
        List<Player> fantasyPlayers,
        DateTime date,
        int season)
    {
        var enrichedPlayers = new List<PlayerGameInfo>();

        foreach (var player in fantasyPlayers)
        {
            if (string.IsNullOrEmpty(player.NbaTeam))
                continue;

            var gamesForDate = await _scheduleService.GetTeamGamesForDateAsync(
                player.NbaTeam,
                date,
                season
            );

            enrichedPlayers.Add(new PlayerGameInfo
            {
                PlayerKey = player.PlayerKey,
                PlayerName = player.FullName,
                Position = player.Position,
                NbaTeam = player.NbaTeam,
                UpcomingGames = gamesForDate
            });
        }

        return enrichedPlayers;
    }

    public async Task<Dictionary<DateTime, List<PlayerGameInfo>>> EnrichFantasyTeamForWeekAsync(
        List<Player> fantasyPlayers,
        DateTime weekStart,
        DateTime weekEnd,
        int season)
    {
        var result = new Dictionary<DateTime, List<PlayerGameInfo>>();

        var currentDate = weekStart;
        while (currentDate <= weekEnd)
        {
            var enrichedForDay = await EnrichFantasyTeamForDateAsync(fantasyPlayers, currentDate, season);
            
            if (enrichedForDay.Any(p => p.PlaysOnDate))
                result[currentDate.Date] = enrichedForDay;

            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    public async Task<RosterLoadSummary> GetRosterLoadForDateAsync(
        List<Player> fantasyPlayers,
        DateTime date,
        int season)
    {
        var enriched = await EnrichFantasyTeamForDateAsync(fantasyPlayers, date, season);

        var playingPlayers = enriched.Where(p => p.PlaysOnDate).ToList();
        var playersWithGames = enriched.GroupBy(p => p.NbaTeam)
            .ToDictionary(
                g => g.Key ?? "Unknown",
                g => g.Count(p => p.PlaysOnDate)
            );

        return new RosterLoadSummary
        {
            Date = date,
            TotalRosterPlayers = fantasyPlayers.Count,
            PlayersWithGames = playingPlayers.Count,
            PlayersNoGame = fantasyPlayers.Count - playingPlayers.Count,
            GamesByTeam = playersWithGames,
            MaxGamesOnDate = enriched.Max(p => p.GameCount)
        };
    }

    public async Task<WeeklyRosterLoadSummary> GetRosterLoadForWeekAsync(
        List<Player> fantasyPlayers,
        DateTime weekStart,
        DateTime weekEnd,
        int season)
    {
        var dailySummaries = new Dictionary<DateTime, RosterLoadSummary>();

        var currentDate = weekStart;
        while (currentDate <= weekEnd)
        {
            dailySummaries[currentDate.Date] = await GetRosterLoadForDateAsync(
                fantasyPlayers,
                currentDate,
                season
            );

            currentDate = currentDate.AddDays(1);
        }

        return new WeeklyRosterLoadSummary
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            DailyLoad = dailySummaries,
            BusiestDay = dailySummaries
                .OrderByDescending(d => d.Value.PlayersWithGames)
                .FirstOrDefault()
                .Key,
            QuietestDay = dailySummaries
                .OrderBy(d => d.Value.PlayersWithGames)
                .FirstOrDefault()
                .Key
        };
    }
}

public class RosterLoadSummary
{
    public DateTime Date { get; set; }
    public int TotalRosterPlayers { get; set; }
    public int PlayersWithGames { get; set; }
    public int PlayersNoGame { get; set; }
    public Dictionary<string, int> GamesByTeam { get; set; } = new();
    public int MaxGamesOnDate { get; set; }

    public double PlayingPercentage => TotalRosterPlayers > 0 
        ? (PlayersWithGames / (double)TotalRosterPlayers) * 100 
        : 0;
}

public class WeeklyRosterLoadSummary
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public Dictionary<DateTime, RosterLoadSummary> DailyLoad { get; set; } = new();
    public DateTime BusiestDay { get; set; }
    public DateTime QuietestDay { get; set; }

    public double AverageDailyPlayingPercentage =>
        DailyLoad.Values.Average(d => d.PlayingPercentage);
}
