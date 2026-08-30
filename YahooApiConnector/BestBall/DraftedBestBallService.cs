using System.Text.Json;

public class DraftedBestBallService
{
    private readonly BestBallEngine _engine = new BestBallEngine();

    public void ProcessWeeklyDrafted(WeeklyLeagueSnapshot snapshot, string dataPath, Dictionary<string, List<string>>? draftedMap = null)
    {
        if (snapshot == null || snapshot.Teams == null)
            return;
        // If caller provided a draftedMap, use it; otherwise load from dataPath/draft_results.json
        if (draftedMap == null)
        {
            draftedMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            // Allow draft_results.json to live in the data root or in a per-season subfolder.
            var candidate = Directory.GetFiles(dataPath, "draft_results.json", SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                try
                {
                    var text = File.ReadAllText(candidate);
                    var drafts = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(text);
                    if (drafts != null)
                    {
                        foreach (var d in drafts)
                        {
                            if (!d.TryGetValue("TeamKey", out var tk) || !d.TryGetValue("PlayerKey", out var pk))
                                continue;

                            var teamKey = tk?.ToString() ?? string.Empty;
                            var playerKey = pk?.ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(teamKey) || string.IsNullOrWhiteSpace(playerKey))
                                continue;

                            if (!draftedMap.ContainsKey(teamKey))
                                draftedMap[teamKey] = new List<string>();

                            draftedMap[teamKey].Add(playerKey);
                        }
                    }
                }
                catch { /* swallow parse errors; treat as no drafts */ }
            }
        }

        // Build player lookup across all teams
        var playerLookup = snapshot.Teams
            .SelectMany(t => t.Players)
            .Where(p => !string.IsNullOrEmpty(p.PlayerKey))
            .ToDictionary(p => p.PlayerKey!, p => p);

        // Use shared processor: supply a function that returns drafted players for a team
        BestBallSharedProcessor.ProcessWeekly(snapshot, dataPath, _engine, team =>
        {
            var tk = team.TeamKey ?? string.Empty;
            var teamPlayers = new List<WeeklyPlayerStats>();

            if (draftedMap.ContainsKey(tk))
            {
                foreach (var pk in draftedMap[tk])
                {
                    if (playerLookup.TryGetValue(pk, out var player))
                    {
                        teamPlayers.Add(player);
                    }
                }
            }

            return teamPlayers;
        }, "best_ball_{season}_week_{week}.json");
    }

    public async Task RebuildSeasonAsync(int season, string dataPath)
    {
        var seasonFolder = Path.Combine(dataPath, season.ToString());
        await BestBallAggregator.RebuildSeasonAsync(season, seasonFolder, $"best_ball_{season}_week_*.json", $"season_best_ball_{season}.json");
        Console.WriteLine($"Drafted season best ball saved to {Path.Combine(seasonFolder, $"season_best_ball_{season}.json")}");
    }
}
