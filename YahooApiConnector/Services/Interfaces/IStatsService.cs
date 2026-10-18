using System;
using System.Collections.Generic;

namespace YahooApiConnector.Services.Interfaces
{
    public interface IStatsService
    {
        Task<Dictionary<string, List<WeeklyPlayerStats>>> GetDailyLeagueResultsAsync(string leagueKey, DateTime date, bool debugStopAfterFirstTeam = false);
        Task<List<WeeklyPlayerStats>> GetFirstTeamAllPlayerStatsForDateAsync(string leagueKey, DateTime date);
        Task<List<TeamWeeklyStats>> GetWeeklyTeamStatsAsync(string leagueKey);
    }
}
