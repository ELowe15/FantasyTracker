public class WeeklyPlayerStats : Player
{
    public Dictionary<string, double> RawStats { get; set; } = new();
    public double FantasyPoints { get; set; } = 0;

    // Eligibility for best ball (true if not IR/IL and position allowed)
    public bool IsEligible { get; set; } = true;

    // Best-ball slot: PG, SG, SF, PF, C, UTIL1, UTIL2, or Bench
    public string BestBallSlot { get; set; } = "Bench";
}