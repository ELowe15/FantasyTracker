public static class RoundRobinService
{
    private static readonly Dictionary<string, CategoryRule> CategoryRules =
        new()
        {
            { "PTS", new CategoryRule { StatId = "12", HigherIsBetter = true } },
            { "REB", new CategoryRule { StatId = "15", HigherIsBetter = true } },
            { "AST", new CategoryRule { StatId = "16", HigherIsBetter = true } },
            { "STL", new CategoryRule { StatId = "17", HigherIsBetter = true } },
            { "BLK", new CategoryRule { StatId = "18", HigherIsBetter = true } },
            { "TO",  new CategoryRule { StatId = "19", HigherIsBetter = false } },
            { "3PM", new CategoryRule { StatId = "10", HigherIsBetter = true } },
            { "FG%", new CategoryRule { StatId = "5", HigherIsBetter = true } },
            { "FT%", new CategoryRule { StatId = "8", HigherIsBetter = true } }
        };

    public static RoundRobinResult RunRoundRobin(List<TeamWeeklyStats> teams)
    {
        if (teams == null || teams.Count < 2)
            throw new ArgumentException("At least two teams are required");

        var teamRecords = teams.ToDictionary(
            t => t.TeamKey,
            t => new TeamRoundRobinRecord { TeamKey = t.TeamKey }
        );

        var matchups = new List<MatchupResult>();

        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                var teamA = teams[i];
                var teamB = teams[j];

                var matchup = new MatchupResult
                {
                    TeamAKey = teamA.TeamKey,
                    TeamBKey = teamB.TeamKey
                };

                foreach (var rule in CategoryRules.Values)
                {
                    var aVal = ParseStat(teamA, rule.StatId);
                    var bVal = ParseStat(teamB, rule.StatId);

                    var cat = new CategoryResult
                    {
                        StatId = rule.StatId,
                        TeamAValue = aVal,
                        TeamBValue = bVal
                    };

                    if (aVal == bVal)
                    {
                        cat.Ties = 1;
                        matchup.CategoryTies++;
                        teamRecords[teamA.TeamKey].CategoryTies++;
                        teamRecords[teamB.TeamKey].CategoryTies++;
                    }
                    else
                    {
                        bool aWins = rule.HigherIsBetter ? aVal > bVal : aVal < bVal;

                        if (aWins)
                        {
                            cat.TeamAWins = 1;
                            matchup.TeamACategoryWins++;
                            teamRecords[teamA.TeamKey].CategoryWins++;
                            teamRecords[teamB.TeamKey].CategoryLosses++;
                        }
                        else
                        {
                            cat.TeamBWins = 1;
                            matchup.TeamBCategoryWins++;
                            teamRecords[teamB.TeamKey].CategoryWins++;
                            teamRecords[teamA.TeamKey].CategoryLosses++;
                        }
                    }

                    matchup.Categories[rule.StatId] = cat;
                }

                if (matchup.TeamACategoryWins > matchup.TeamBCategoryWins)
                {
                    teamRecords[teamA.TeamKey].MatchupWins++;
                    teamRecords[teamB.TeamKey].MatchupLosses++;
                }
                else if (matchup.TeamACategoryWins < matchup.TeamBCategoryWins)
                {
                    teamRecords[teamB.TeamKey].MatchupWins++;
                    teamRecords[teamA.TeamKey].MatchupLosses++;
                }
                else
                {
                    teamRecords[teamA.TeamKey].MatchupTies++;
                    teamRecords[teamB.TeamKey].MatchupTies++;
                }

                matchups.Add(matchup);
            }
        }

        return new RoundRobinResult
        {
            TeamRecords = teamRecords.Values.ToList(),
            Matchups = matchups
        };
    }

    private static double ParseStat(TeamWeeklyStats team, string statId)
    {
        if (team.StatValues == null ||
            !team.StatValues.TryGetValue(statId, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
            return 0;

        raw = raw.Trim();

        // Handle ratio stats: "152/330"
        if (raw.Contains('/'))
        {
            var parts = raw.Split('/');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out var made) &&
                double.TryParse(parts[1], out var att) &&
                att != 0)
            {
                return made / att;
            }

            return 0;
        }

        return double.TryParse(raw, out var val) ? val : 0;
    }
}


public class RoundRobinResult
{
    public List<TeamRoundRobinRecord> TeamRecords { get; set; } = new();
    public List<MatchupResult> Matchups { get; set; } = new();
}

public class TeamRoundRobinRecord
{
    public string TeamKey { get; set; } = string.Empty;

    public int MatchupWins { get; set; }
    public int MatchupLosses { get; set; }
    public int MatchupTies { get; set; }

    public int CategoryWins { get; set; }
    public int CategoryLosses { get; set; }
    public int CategoryTies { get; set; }
}

public class MatchupResult
{
    public string TeamAKey { get; set; } = string.Empty;
    public string TeamBKey { get; set; } = string.Empty;

    public int TeamACategoryWins { get; set; }
    public int TeamBCategoryWins { get; set; }
    public int CategoryTies { get; set; }

    public Dictionary<string, CategoryResult> Categories { get; set; } = new();
}

public class CategoryResult
{
    public string StatId { get; set; } = string.Empty;

    public double TeamAValue { get; set; }
    public double TeamBValue { get; set; }

    public int TeamAWins { get; set; }
    public int TeamBWins { get; set; }
    public int Ties { get; set; }
}

public class CategoryRule
{
    public string StatId { get; set; } = string.Empty;

    // true = higher stat wins (PTS, REB, AST)
    // false = lower stat wins (TO)
    public bool HigherIsBetter { get; set; }
}




