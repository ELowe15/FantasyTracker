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

        // Percentages — use raw data
        {
            "FG%",
            new CategoryRule
            {
                HigherIsBetter = true,
                MakesStatKey = "FGM",
                AttemptsStatKey = "A"
            }
        },
        {
            "FT%",
            new CategoryRule
            {
                HigherIsBetter = true,
                MakesStatKey = "FTM",
                AttemptsStatKey = "A"
            }
        }
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
                    double teamVal;
                    double oppVal;

                    if (rule.MakesStatKey != null && rule.AttemptsStatKey != null)
                    {
                        var teamMakes = ParseStat(team, rule.MakesStatKey);
                        var teamAtt   = ParseStat(team, rule.AttemptsStatKey);
                        var oppMakes  = ParseStat(opponent, rule.MakesStatKey);
                        var oppAtt    = ParseStat(opponent, rule.AttemptsStatKey);

                        teamVal = teamAtt > 0 ? teamMakes / teamAtt : 0;
                        oppVal  = oppAtt  > 0 ? oppMakes  / oppAtt  : 0;
                    }
                    else
                    {
                        teamVal = ParseStat(team, statKey);
                        oppVal  = ParseStat(opponent, statKey);
                    }


                    var categoryRecord = result.TeamRecord.CategoryRecords[statKey];

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

                // Determine matchup outcome
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

    private static double ParseStat(TeamWeeklyStats team, string statId)
    {
        if (team.StatValues == null ||
            !team.StatValues.TryGetValue(statId, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
            return 0;

        raw = raw.Trim();

        // Ratio stats like "152/330"
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

public class CategoryRule
{
    public bool HigherIsBetter { get; set; }

    // Optional: only relevant for FG% / FT%
    public string? MakesStatKey { get; set; }
    public string? AttemptsStatKey { get; set; }
}
