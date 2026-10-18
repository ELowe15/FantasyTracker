public class RoundRobinResult
{
    public string TeamKey { get; set; } = string.Empty;
    public TeamWeeklyStats Team { get; set; } = new();
    public TeamRoundRobinRecord TeamRecord { get; set; } = new();
    public List<MatchupResult> Matchups { get; set; } = new();
}
