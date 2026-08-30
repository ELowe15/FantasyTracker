public class TeamRoundRobinRecord
{
    public int MatchupWins { get; set; }
    public int MatchupLosses { get; set; }
    public int MatchupTies { get; set; }

    public int CategoryWins { get; set; }
    public int CategoryLosses { get; set; }
    public int CategoryTies { get; set; }

    public Dictionary<string, CategoryRecord> CategoryRecords { get; set; }
        = new();
}
