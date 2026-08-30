using System.Text.Json;

public static class BestBallAggregator
{
    public static void WriteWeeklySnapshot(WeeklyLeagueSnapshot snapshot, string seasonFolder, string fileName)
    {
        if (!Directory.Exists(seasonFolder))
            Directory.CreateDirectory(seasonFolder);

        var outPath = Path.Combine(seasonFolder, fileName);
        var options = new JsonSerializerOptions { WriteIndented = true };
        var outJson = JsonSerializer.Serialize(snapshot, options);
        File.WriteAllText(outPath, outJson);
    }

    public static async Task RebuildSeasonAsync(int season, string seasonFolder, string weeklyPattern, string seasonOutputFileName)
    {
        if (!Directory.Exists(seasonFolder))
            return;

        var weeklyFiles = Directory.GetFiles(seasonFolder, weeklyPattern).OrderBy(f => f).ToList();
        if (weeklyFiles.Count == 0)
            return;

        var teamScoreMap = new Dictionary<string, List<double>>();
        var teamRankMap = new Dictionary<string, TeamRankTracker>();
        var teamPlayerMap = new Dictionary<string, Dictionary<string, SeasonBestBallPlayer>>();
        var managerLookup = new Dictionary<string, string>();
        var weeksIncluded = new HashSet<int>();

        foreach (var file in weeklyFiles)
        {
            var json = await File.ReadAllTextAsync(file);
            var snapshot = JsonSerializer.Deserialize<WeeklyLeagueSnapshot>(json);
            if (snapshot == null || snapshot.Teams == null)
                continue;

            weeksIncluded.Add(snapshot.Week);

            var rankedTeams = snapshot.Teams.OrderByDescending(t => t.TotalBestBallPoints).ToList();
            for (int i = 0; i < rankedTeams.Count; i++)
            {
                var team = rankedTeams[i];
                var rank = i + 1;

                if (!teamScoreMap.ContainsKey(team.TeamKey))
                    teamScoreMap[team.TeamKey] = new List<double>();

                if (!teamRankMap.ContainsKey(team.TeamKey))
                    teamRankMap[team.TeamKey] = new TeamRankTracker();

                if (!managerLookup.ContainsKey(team.TeamKey))
                    managerLookup[team.TeamKey] = team.ManagerName;

                if (!teamPlayerMap.ContainsKey(team.TeamKey))
                    teamPlayerMap[team.TeamKey] = new Dictionary<string, SeasonBestBallPlayer>();

                teamScoreMap[team.TeamKey].Add(team.TotalBestBallPoints);
                teamRankMap[team.TeamKey].TotalRankPoints += rank;

                foreach (var player in team.Players)
                {
                    var pk = player.PlayerKey ?? string.Empty;
                    if (!teamPlayerMap[team.TeamKey].ContainsKey(pk))
                    {
                        teamPlayerMap[team.TeamKey][pk] = new SeasonBestBallPlayer
                        {
                            PlayerKey = pk,
                            PlayerName = player.FullName
                        };
                    }

                    var seasonPlayer = teamPlayerMap[team.TeamKey][pk];
                    seasonPlayer.WeeksOnRoster++;
                    if (player.BestBallSlot != "Bench")
                    {
                        seasonPlayer.WeeksStarted++;
                        seasonPlayer.TotalContributedPoints += player.FantasyPoints;
                    }
                }
            }
        }

        var seasonSnapshot = new SeasonBestBallSnapshot
        {
            Season = season,
            LastUpdated = DateTime.UtcNow,
            WeeksIncluded = weeksIncluded.OrderBy(w => w).ToList(),
            Teams = new List<SeasonBestBallTeam>()
        };

        foreach (var kv in teamScoreMap)
        {
            var teamKey = kv.Key;
            var scores = kv.Value;

            var team = new SeasonBestBallTeam
            {
                TeamKey = teamKey,
                ManagerName = managerLookup[teamKey],
                WeeksPlayed = scores.Count,
                SeasonTotalBestBallPoints = scores.Sum(),
                BestWeekScore = scores.Max(),
                WorstWeekScore = scores.Min(),
                TotalRankPoints = teamRankMap[teamKey].TotalRankPoints,
                AverageRank = scores.Count == 0 ? 0 : (double)teamRankMap[teamKey].TotalRankPoints / scores.Count
            };

            if (teamPlayerMap.ContainsKey(teamKey))
            {
                foreach (var player in teamPlayerMap[teamKey].Values)
                {
                    player.ContributionPercent = team.SeasonTotalBestBallPoints == 0 ? 0 : player.TotalContributedPoints / team.SeasonTotalBestBallPoints;
                }

                team.Players = teamPlayerMap[teamKey].Values.OrderByDescending(p => p.TotalContributedPoints).Take(10).ToList();
            }

            seasonSnapshot.Teams.Add(team);
        }

        seasonSnapshot.Teams = seasonSnapshot.Teams.OrderByDescending(t => t.SeasonTotalBestBallPoints).ToList();

        var outPath = Path.Combine(seasonFolder, seasonOutputFileName);
        var options = new JsonSerializerOptions { WriteIndented = true };
        var outputJson = JsonSerializer.Serialize(seasonSnapshot, options);
        await File.WriteAllTextAsync(outPath, outputJson);
    }
}

public class TeamRankTracker
{
    public int TotalRankPoints { get; set; }
}
 
