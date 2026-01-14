public static class RoundRobinService
{
    private static readonly Dictionary<string, CategoryRule> CategoryRules =
    new()
    {
        { "PTS", new CategoryRule { HigherIsBetter = true } },
        { "REB", new CategoryRule { HigherIsBetter = true } },
        { "AST", new CategoryRule { HigherIsBetter = true } },
        { "STL", new CategoryRule { HigherIsBetter = true } },
        { "BLK", new CategoryRule { HigherIsBetter = true } },
        { "TO",  new CategoryRule { HigherIsBetter = false } },
        { "3PM", new CategoryRule { HigherIsBetter = true } },
        { "FGM/A",  new CategoryRule { HigherIsBetter = false } }, //Save as FG%
        { "FTM/A", new CategoryRule { HigherIsBetter = true } } //Save as FT%
    };


   public static List<RoundRobinResult> RunRoundRobin(List<TeamWeeklyStats> teams)
{
    if (teams == null || teams.Count < 2)
        throw new ArgumentException("At least two teams are required");

    // Initialize results per team
    var results = teams.ToDictionary(
        t => t.TeamKey,
        t =>
        {
            var record = new TeamRoundRobinRecord();

            foreach (var category in CategoryRules.Keys)
            {
                record.CategoryRecords[category] = new CategoryRecord
                {
                    Category = category
                };
            }

            return new RoundRobinResult
            {
                TeamKey = t.TeamKey,
                Team = t,
                TeamRecord = record,
                Matchups = new List<MatchupResult>()
            };
        }
    );

    // Each team evaluates itself vs every other team
    foreach (var team in teams)
    {
        var result = results[team.TeamKey];

        foreach (var opponent in teams)
        {
            if (team.TeamKey == opponent.TeamKey)
                continue;

            var matchup = new MatchupResult
            {
                OpponentTeamKey = opponent.TeamKey,
                ManagerName = opponent.ManagerName
            };

            foreach (var (statKey, rule) in CategoryRules)
            {
                double teamVal = ParseRatioStat(team, statKey);
                double oppVal = ParseRatioStat(opponent, statKey);

                // Save the calculated value back for display (high precision)
                team.StatValues[statKey] = teamVal.ToString("F5");

                var categoryRecord = result.TeamRecord.CategoryRecords[statKey];

                // Determine W/L/T
                if (teamVal == oppVal)
                {
                    matchup.CategoryTies++;
                    result.TeamRecord.CategoryTies++;
                    categoryRecord.Ties++;
                }
                else
                {
                    bool teamWins = rule.HigherIsBetter
                        ? teamVal > oppVal
                        : teamVal < oppVal;

                    if (teamWins)
                    {
                        matchup.CategoryWins++;
                        result.TeamRecord.CategoryWins++;
                        categoryRecord.Wins++;
                    }
                    else
                    {
                        matchup.OpponentCategoryWins++;
                        result.TeamRecord.CategoryLosses++;
                        categoryRecord.Losses++;
                    }
                }
            }

            // Determine overall matchup outcome
            if (matchup.CategoryWins > matchup.OpponentCategoryWins)
            {
                result.TeamRecord.MatchupWins++;
            }
            else if (matchup.CategoryWins < matchup.OpponentCategoryWins)
            {
                result.TeamRecord.MatchupLosses++;
            }
            else
            {
                result.TeamRecord.MatchupTies++;
            }

            result.Matchups.Add(matchup);
        }
    }

    return results.Values.ToList();
}

// Parse FGM/A or FTM/A ratio stats
private static double ParseRatioStat(TeamWeeklyStats team, string statKey)
{
    if (team.StatValues == null ||
        !team.StatValues.TryGetValue(statKey, out var raw) ||
        string.IsNullOrWhiteSpace(raw))
        return 0;

    raw = raw.Trim();

    if (raw.Contains('/'))
    {
        var parts = raw.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], out var made) &&
            double.TryParse(parts[1], out var attempts) &&
            attempts != 0)
        {
            return made / attempts; // high-precision division
        }

        return 0;
    }

    // For normal stats like PTS, REB, etc.
    return double.TryParse(raw, out var val) ? val : 0;
}

}

public class CategoryRule
{
    public bool HigherIsBetter { get; set; }

    // Optional: only relevant for FG% / FT%
    public string? MakesStatKey { get; set; }
    public string? AttemptsStatKey { get; set; }
}
