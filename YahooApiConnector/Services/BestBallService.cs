public class BestBallService
{
    private readonly List<string> _positionSlots = new() { "PG", "SG", "SF", "PF", "C", "UTIL1", "UTIL2" };

    private void ConvertStatsToFantasyPoints(WeeklyPlayerStats player, Dictionary<string, double> scoringSettings)
    {
        double total = 0;
        foreach (var kv in player.RawStats)
            if (scoringSettings.TryGetValue(kv.Key, out double multiplier))
                total += kv.Value * multiplier;

        player.FantasyPoints = total;
    }

    private void AssignBestBallLineup(List<WeeklyPlayerStats> players)
    {
        foreach (var p in players)
            p.BestBallSlot = "Bench"; // reset

        var eligible = players.Where(p => p.IsEligible).OrderByDescending(p => p.FantasyPoints).ToList();

        // Fill each core position
        foreach (var pos in new[] { "PG", "SG", "SF", "PF", "C" })
        {
            var candidate = eligible.FirstOrDefault(p => p.Position == pos && p.BestBallSlot == "Bench");
            if (candidate != null) candidate.BestBallSlot = pos;
        }

        // Fill UTIL
        var utilSlots = new[] { "UTIL1", "UTIL2" };
        int utilIndex = 0;
        foreach (var player in eligible)
        {
            if (player.BestBallSlot == "Bench" && utilIndex < utilSlots.Length)
            {
                player.BestBallSlot = utilSlots[utilIndex];
                utilIndex++;
            }
        }
    }
    

    public void ProcessWeeklyBestBall(List<WeeklyTeamResult> teams)
    {
        foreach (var team in teams)
        {
            //foreach (var player in team.Players)
                //ConvertStatsToFantasyPoints(player, scoring);

            AssignBestBallLineup(team.Players);

            team.TotalBestBallPoints = team.Players
                .Where(p => p.BestBallSlot != "Bench")
                .Sum(p => p.FantasyPoints);
        }
    }
}
