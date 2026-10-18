public static class BestBallSharedProcessor
{
    public static void ProcessWeekly(WeeklyLeagueSnapshot snapshot, string dataPath, string categoryFolder, BestBallEngine engine, Func<WeeklyTeamResult, List<WeeklyPlayerStats>> getPlayersForTeam, string weeklyFileNameFormat)
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

        foreach (var team in snapshot.Teams)
        {
            var teamPlayers = getPlayersForTeam(team) ?? new List<WeeklyPlayerStats>();
            engine.AssignBestBallLineup(teamPlayers);

            var teamOut = new WeeklyTeamResult
            {
                Season = team.Season,
                Week = team.Week,
                TeamKey = team.TeamKey ?? string.Empty,
                ManagerName = team.ManagerName,
                Players = teamPlayers,
                TotalBestBallPoints = teamPlayers.Where(p => p.BestBallSlot != "Bench").Sum(p => p.FantasyPoints)
            };

            outSnapshot.Teams.Add(teamOut);
        }

        var outputFolder = Path.Combine(dataPath, snapshot.Season.ToString(), categoryFolder);
        var fileName = weeklyFileNameFormat.Replace("{season}", snapshot.Season.ToString()).Replace("{week}", snapshot.Week.ToString());
        BestBallAggregator.WriteWeeklySnapshot(outSnapshot, outputFolder, fileName);
        Console.WriteLine($"Best ball weekly results saved to {Path.Combine(outputFolder, fileName)}");
    }
}
