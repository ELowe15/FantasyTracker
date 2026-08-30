using System.Text.Json;

public class BestBallOrchestrator
{
    private readonly DraftedBestBallService _draftedService;
    private readonly PeakLineupService _peakService;

    public BestBallOrchestrator(DraftedBestBallService? drafted = null, PeakLineupService? peak = null)
    {
        _draftedService = drafted ?? new DraftedBestBallService();
        _peakService = peak ?? new PeakLineupService();
    }

    public async Task ProcessWeeklyAllModesAsync(WeeklyLeagueSnapshot snapshot, string dataPath)
    {
        if (snapshot == null)
            return;

        // Cache draft results once for the run (if present)
        var draftMap = LoadDraftedMapIfExists(dataPath);

        // Ensure season folder exists
        var seasonFolder = Path.Combine(dataPath, snapshot.Season.ToString());
        if (!Directory.Exists(seasonFolder))
            Directory.CreateDirectory(seasonFolder);

        // Run weekly processing in parallel (they are independent)
        var draftedTask = Task.Run(() => _draftedService.ProcessWeeklyDrafted(snapshot, dataPath, draftMap));
        var peakTask = Task.Run(() => _peakService.ProcessWeeklyPeak(snapshot, dataPath));

        await Task.WhenAll(draftedTask, peakTask);

        // Rebuild season aggregates in parallel
        var rebuildDrafted = _draftedService.RebuildSeasonAsync(snapshot.Season, dataPath);
        var rebuildPeak = _peakService.RebuildSeasonAsync(snapshot.Season, dataPath);

        await Task.WhenAll(rebuildDrafted, rebuildPeak);
    }

    private Dictionary<string, List<string>>? LoadDraftedMapIfExists(string dataPath)
    {
        // Allow draft_results.json to live in the data root or in a per-season subfolder.
        var candidate = Directory.GetFiles(dataPath, "draft_results.json", SearchOption.AllDirectories).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
            return null;

        try
        {
            var text = File.ReadAllText(candidate);
            var drafts = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(text);
            if (drafts == null)
                return null;

            var draftedMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
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

            return draftedMap;
        }
        catch
        {
            return null;
        }
    }
}
