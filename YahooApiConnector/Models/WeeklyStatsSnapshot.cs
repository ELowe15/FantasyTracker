public class WeeklyStatsSnapshot
{
    public int Season { get; set; }
    public int Week { get; set; }

    public List<RoundRobinResult> RoundRobinResults { get; set; } = new();
}

public class SeasonRoundRobinSnapshot
{
    public int Season { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<int> WeeksIncluded { get; set; } = new();
    public List<RoundRobinResult> RoundRobinResults { get; set; } = new();
}

public class RoundRobinResult
{
    public string TeamKey { get; set; } = string.Empty;
    public TeamWeeklyStats Team { get; set; } = new();
    public TeamRoundRobinRecord TeamRecord { get; set; } = new();
    public List<MatchupResult> Matchups { get; set; } = new();
}

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

public class CategoryRecord
{
    public string Category { get; set; } = "";

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
}



public class MatchupResult
{
    public string OpponentTeamKey { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;

    public int CategoryWins { get; set; }
    public int OpponentCategoryWins { get; set; }
    public int CategoryTies { get; set; }
}

