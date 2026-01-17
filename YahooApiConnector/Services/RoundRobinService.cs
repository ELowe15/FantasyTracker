using System.Text.Json;


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
                if (statKey == "FGM/A" || statKey == "FTM/A")
                {
                    // Save the calculated value back for display (high precision)
                    team.StatValues[statKey] = teamVal.ToString("F5");
                }
                else
                {
                    team.StatValues[statKey] = teamVal.ToString("F0");
                }
                
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
                matchup.Wins++;
            }
            else if (matchup.CategoryWins < matchup.OpponentCategoryWins)
            {
                result.TeamRecord.MatchupLosses++;
                matchup.Losses++;
            }
            else
            {
                result.TeamRecord.MatchupTies++;
                matchup.Ties++;
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
    

       public static async Task RebuildSeasonRoundRobinAsync(
            int season,
            string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = Directory.GetCurrentDirectory();

            if (!Directory.Exists(outputDirectory))
                return;

            var weeklyFiles = Directory
                .GetFiles(outputDirectory, $"round_robin_{season}_week_*.json")
                .OrderBy(f => f)
                .ToList();

            if (weeklyFiles.Count == 0)
                return;

            var seasonResults = new Dictionary<string, RoundRobinResult>();
            var weeksIncluded = new HashSet<int>();

            foreach (var file in weeklyFiles)
            {
                var json = await File.ReadAllTextAsync(file);
                var snapshot = JsonSerializer.Deserialize<WeeklyStatsSnapshot>(json);

                if (snapshot?.RoundRobinResults == null)
                    continue;

                weeksIncluded.Add(snapshot.Week);

                foreach (var weeklyResult in snapshot.RoundRobinResults)
                {
                    if (!seasonResults.TryGetValue(weeklyResult.TeamKey, out var seasonResult))
                    {
                        // Clone shell (no stats, no matchups)
                        seasonResult = new RoundRobinResult
                        {
                            TeamKey = weeklyResult.TeamKey,
                            Team = weeklyResult.Team,
                            TeamRecord = new TeamRoundRobinRecord(),
                            Matchups = new List<MatchupResult>()
                        };

                        // Initialize category records
                        foreach (var category in CategoryRules.Keys)
                        {
                            seasonResult.TeamRecord.CategoryRecords[category] =
                                new CategoryRecord { Category = category };
                        }

                        seasonResults[weeklyResult.TeamKey] = seasonResult;
                    }

                    var sr = seasonResult.TeamRecord;
                    var wr = weeklyResult.TeamRecord;

                    // ---- Aggregate matchup totals ----
                    sr.MatchupWins   += wr.MatchupWins;
                    sr.MatchupLosses += wr.MatchupLosses;
                    sr.MatchupTies   += wr.MatchupTies;

                    // ---- Aggregate category totals ----
                    sr.CategoryWins   += wr.CategoryWins;
                    sr.CategoryLosses += wr.CategoryLosses;
                    sr.CategoryTies   += wr.CategoryTies;

                    foreach (var (cat, weeklyCat) in wr.CategoryRecords)
                    {
                        var seasonCat = sr.CategoryRecords[cat];
                        seasonCat.Wins   += weeklyCat.Wins;
                        seasonCat.Losses += weeklyCat.Losses;
                        seasonCat.Ties   += weeklyCat.Ties;
                    }

                    // ---- Aggregate matchup-by-opponent ----
                    foreach (var weeklyMatchup in weeklyResult.Matchups)
                    {
                        var seasonMatchup = seasonResult.Matchups
                            .FirstOrDefault(m => m.OpponentTeamKey == weeklyMatchup.OpponentTeamKey);

                        if (seasonMatchup == null)
                        {
                            seasonMatchup = new MatchupResult
                            {
                                OpponentTeamKey = weeklyMatchup.OpponentTeamKey,
                                ManagerName = weeklyMatchup.ManagerName,
                                CategoryWins = 0,
                                OpponentCategoryWins = 0,
                                CategoryTies = 0
                            };

                            seasonResult.Matchups.Add(seasonMatchup);
                        }

                        seasonMatchup.CategoryWins += weeklyMatchup.CategoryWins;
                        seasonMatchup.OpponentCategoryWins += weeklyMatchup.OpponentCategoryWins;
                        seasonMatchup.CategoryTies += weeklyMatchup.CategoryTies;
                        seasonMatchup.Wins   += weeklyMatchup.Wins;
                        seasonMatchup.Losses += weeklyMatchup.Losses;
                        seasonMatchup.Ties   += weeklyMatchup.Ties;
                    }

                }
            }

            var seasonSnapshot = new SeasonRoundRobinSnapshot
            {
                Season = season,
                WeeksIncluded = weeksIncluded.OrderBy(w => w).ToList(),
                LastUpdated = DateTime.UtcNow,
                RoundRobinResults = seasonResults.Values
                    .OrderByDescending(r => r.TeamRecord.MatchupWins)
                    .ThenByDescending(r => r.TeamRecord.MatchupTies)
                    .ThenBy(r => r.TeamRecord.MatchupLosses)
                    .ToList()
            };

            var outputPath = Path.Combine(
                outputDirectory,
                $"season_round_robin_{season}.json"
            );

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            await File.WriteAllTextAsync(
                outputPath,
                JsonSerializer.Serialize(seasonSnapshot, options)
            );
        }

}

public class CategoryRule
{
    public bool HigherIsBetter { get; set; }

    // Optional: only relevant for FG% / FT%
    public string? MakesStatKey { get; set; }
    public string? AttemptsStatKey { get; set; }
}
