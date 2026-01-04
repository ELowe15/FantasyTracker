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
    // Reset all slots
    foreach (var p in players)
        p.BestBallSlot = "Bench";

    // Eligible players, sorted by descending fantasy points
    var eligible = players.Where(p => p.IsEligible).OrderByDescending(p => p.FantasyPoints).ToList();

    // Core positions in order
    var corePositions = new[] { "PG", "SG", "SF", "PF", "C" };

    foreach (var pos in corePositions)
    {
        // Find the highest fantasy points player who can play this position and is still on bench
        var candidate = eligible.FirstOrDefault(p =>
        {
            var positions = p.Position.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return positions.Contains(pos) && p.BestBallSlot == "Bench";
        });

        if (candidate != null)
            candidate.BestBallSlot = pos;
    }

    // Fill UTIL slots with remaining highest FP players
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
