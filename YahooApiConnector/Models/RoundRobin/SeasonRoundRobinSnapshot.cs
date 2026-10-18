public class SeasonRoundRobinSnapshot
{
    public int Season { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<int> WeeksIncluded { get; set; } = new();
    public List<RoundRobinResult> RoundRobinResults { get; set; } = new();
}
