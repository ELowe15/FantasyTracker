namespace NbaSchedule.Helpers;

public static class DateRangeHelper
{
    public static (DateTime Start, DateTime End) GetWeekDateRange(DateTime dateInWeek)
    {
        var dayOfWeek = (int)dateInWeek.DayOfWeek;
        var daysUntilMonday = (dayOfWeek - 1 + 7) % 7;
        var weekStart = dateInWeek.AddDays(-daysUntilMonday).Date;
        var weekEnd = weekStart.AddDays(6);

        return (weekStart, weekEnd);
    }

    public static bool IsDateInNbaSeason(DateTime date, int season)
    {
        var seasonStartYear = season;
        var seasonStartMonth = 10;
        var seasonStartDay = 1;

        var seasonEndYear = season + 1;
        var seasonEndMonth = 6;
        var seasonEndDay = 30;

        var seasonStart = new DateTime(seasonStartYear, seasonStartMonth, seasonStartDay);
        var seasonEnd = new DateTime(seasonEndYear, seasonEndMonth, seasonEndDay);

        return date >= seasonStart && date <= seasonEnd;
    }

    public static int GetNbaSeasonForDate(DateTime date)
    {
        if (date.Month >= 10)
            return date.Year;
        else
            return date.Year - 1;
    }

    public static int GetNbaWeekForDate(DateTime date, int season)
    {
        if (!IsDateInNbaSeason(date, season))
            return -1;

        var seasonStart = new DateTime(season, 10, 1);
        var daysIntoSeason = (date - seasonStart).Days;
        var week = (daysIntoSeason / 7) + 1;
        
        return Math.Max(1, week);
    }

    public static List<DateTime> GetGameDaysInRange(
        List<DateTime> allGameTimes,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        return allGameTimes
            .Where(g => g.Date >= rangeStart.Date && g.Date <= rangeEnd.Date)
            .Select(g => g.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }

    public static Dictionary<string, int> CountGamesByTeam(
        List<(string Team, DateTime GameTime)> games,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var result = new Dictionary<string, int>();

        foreach (var (team, gameTime) in games)
        {
            if (gameTime.Date >= rangeStart.Date && gameTime.Date <= rangeEnd.Date)
            {
                if (!result.ContainsKey(team))
                    result[team] = 0;
                result[team]++;
            }
        }

        return result.OrderByDescending(kv => kv.Value)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public static (List<DateTime> Heavy, List<DateTime> Light) IdentifyLoadDays(
        List<(string HomeTeam, string AwayTeam, DateTime GameTime)> games,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var gamesByDate = new Dictionary<DateTime, HashSet<string>>();

        foreach (var (home, away, gameTime) in games)
        {
            var date = gameTime.Date;
            if (date >= rangeStart.Date && date <= rangeEnd.Date)
            {
                if (!gamesByDate.ContainsKey(date))
                    gamesByDate[date] = new HashSet<string>();

                gamesByDate[date].Add(home);
                gamesByDate[date].Add(away);
            }
        }

        var heavyDays = gamesByDate
            .Where(kv => kv.Value.Count >= 6)
            .Select(kv => kv.Key)
            .OrderBy(d => d)
            .ToList();

        var lightDays = gamesByDate
            .Where(kv => kv.Value.Count <= 2)
            .Select(kv => kv.Key)
            .OrderBy(d => d)
            .ToList();

        return (heavyDays, lightDays);
    }
}
