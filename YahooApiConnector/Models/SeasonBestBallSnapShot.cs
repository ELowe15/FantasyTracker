public class SeasonBestBallSnapshot
{
    public int Season { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<int> WeeksIncluded { get; set; } = new();
    public List<SeasonBestBallTeam> Teams { get; set; } = new();
}