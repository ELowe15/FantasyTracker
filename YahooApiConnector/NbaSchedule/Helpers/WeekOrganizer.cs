using NbaSchedule.Models;

namespace NbaSchedule.Helpers;

public static class WeekOrganizer
{
    public static List<NbaWeekSchedule> OrganizeGamesByWeek(List<NbaGame> games, int season)
    {
        if (games.Count == 0)
            return new List<NbaWeekSchedule>();

        var sortedGames = games.OrderBy(g => g.GameTime).ToList();
        var firstGame = sortedGames.First();
        var lastGame = sortedGames.Last();

        var weekStart = GetMondayOfWeek(firstGame.GameTime);
        var weekSchedules = new List<NbaWeekSchedule>();
        var weekNumber = 1;

        while (weekStart <= lastGame.GameTime)
        {
            var weekEnd = weekStart.AddDays(6);
            var gamesInWeek = sortedGames
                .Where(g => g.GameTime.Date >= weekStart.Date && g.GameTime.Date <= weekEnd.Date)
                .ToList();

            if (gamesInWeek.Count > 0)
            {
                var weekSchedule = new NbaWeekSchedule
                {
                    Week = new NbaWeek
                    {
                        WeekNumber = weekNumber,
                        StartDate = weekStart,
                        EndDate = weekEnd,
                        Season = season
                    },
                    Games = gamesInWeek
                };
                weekSchedules.Add(weekSchedule);
                weekNumber++;
            }

            weekStart = weekStart.AddDays(7);
        }

        return weekSchedules;
    }

    private static DateTime GetMondayOfWeek(DateTime date)
    {
        var dayOfWeek = date.DayOfWeek;
        var daysToMonday = dayOfWeek == DayOfWeek.Sunday ? 1 : (int)dayOfWeek == 0 ? 1 : (int)dayOfWeek * -1 + 1;
        return date.AddDays(daysToMonday).Date;
    }

    public static int GetWeekNumber(DateTime date, DateTime seasonStart)
    {
        var mondayOfDate = GetMondayOfWeek(date);
        var mondayOfStart = GetMondayOfWeek(seasonStart);
        var weeksDifference = (mondayOfDate - mondayOfStart).Days / 7;
        return weeksDifference + 1;
    }
}
