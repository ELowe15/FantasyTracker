using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YahooApiConnector.Services.Interfaces
{
    public interface ISnapshotService
    {
        Task<WeeklyLeagueSnapshot> InitializeNewWeeklySnapshot(string leagueKey, int season, int week, DateTime weekStart, DateTime weekEnd);
        void MergeDailyIntoWeekly(WeeklyLeagueSnapshot snapshot, Dictionary<string, List<WeeklyPlayerStats>> dailyResults);
        List<int> GetAvailableWeeks(int season, string directoryPath);
        Task WriteWeeklySnapshotAsync(string path, WeeklyLeagueSnapshot snapshot);
    }
}
