public class SeasonBestBallPlayer
{
    public string PlayerKey { get; set; }
    public string PlayerName { get; set; }

    public int WeeksOnRoster { get; set; }
    public int WeeksStarted { get; set; }

    public double TotalContributedPoints { get; set; }

    public double StartRate =>
        WeeksOnRoster == 0 ? 0 :
        (double)WeeksStarted / WeeksOnRoster;

    public double PointsPerStart =>
        WeeksStarted == 0 ? 0 :
        TotalContributedPoints / WeeksStarted;

    public double ContributionPercent { get; set; }
}
