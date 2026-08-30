public class MatchupResult
{
    public string OpponentTeamKey { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }

    public int CategoryWins { get; set; }
    public int OpponentCategoryWins { get; set; }
    public int CategoryTies { get; set; }
}
