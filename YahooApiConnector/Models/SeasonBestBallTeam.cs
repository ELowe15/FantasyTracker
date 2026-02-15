public class SeasonBestBallTeam
{
    public string TeamKey { get; set; }
    public string ManagerName { get; set; }

    public int WeeksPlayed { get; set; }

    public double SeasonTotalBestBallPoints { get; set; }

    public int TotalRankPoints { get; set; }
    public double AverageRank { get; set; }

    public double BestWeekScore { get; set; }
    public double WorstWeekScore { get; set; }

    public List<SeasonBestBallPlayer> Players { get; set; } = new();
}
