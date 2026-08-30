using YahooApiConnector.Services.Interfaces;

namespace YahooApiConnector.Services;

public class SnapshotService : ISnapshotService
{
    private readonly YahooFantasyApiClient _apiClient;

    public SnapshotService(YahooFantasyApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<WeeklyLeagueSnapshot> InitializeNewWeeklySnapshot(string leagueKey, int season, int week, DateTime weekStart, DateTime weekEnd)
    {
        var snapshot = new WeeklyLeagueSnapshot
        {
            Season = season,
            Week = week,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Teams = new List<WeeklyTeamResult>(),
            ProcessedDates = new HashSet<DateTime>()
        };

        var teams = (await _apiClient.GetLeagueTeamsAsync(leagueKey))
            .Select(t => new WeeklyTeamResult
            {
                TeamKey = Helpers.Hash(t.TeamKey),
                ManagerName = Helpers.GetDisplayManagerName(t.ManagerName),
                Week = week,
                Players = new List<WeeklyPlayerStats>()
            })
            .ToList();

        snapshot.Teams = teams;

        return snapshot;
    }

    public void MergeDailyIntoWeekly(WeeklyLeagueSnapshot snapshot, Dictionary<string, List<WeeklyPlayerStats>> dailyResults)
    {
        foreach (var team in snapshot.Teams)
        {
            if (!dailyResults.ContainsKey(team.TeamKey))
                continue;

            foreach (var dailyPlayer in dailyResults[team.TeamKey])
            {
                var existing = team.Players.FirstOrDefault(p => p.PlayerKey == dailyPlayer.PlayerKey);

                if (existing == null)
                {
                    existing = new WeeklyPlayerStats
                    {
                        PlayerKey = dailyPlayer.PlayerKey,
                        FullName = dailyPlayer.FullName,
                        Position = dailyPlayer.Position,
                        NbaTeam = dailyPlayer.NbaTeam,
                        RawStats = new Dictionary<string, double>()
                    };
                    team.Players.Add(existing);
                }

                foreach (var stat in dailyPlayer.RawStats)
                {
                    if (stat.Key == "FG%" || stat.Key == "FT%")
                        continue;

                    if (!existing.RawStats.ContainsKey(stat.Key))
                        existing.RawStats[stat.Key] = 0;

                    existing.RawStats[stat.Key] += stat.Value;
                }

                existing.FantasyPoints = Helpers.ComputeFantasyPoints(existing.RawStats);
            }
        }
    }

    public List<int> GetAvailableWeeks(int season, string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            directoryPath = Directory.GetCurrentDirectory();

        if (!Directory.Exists(directoryPath))
            return new List<int>();

        // Look in the provided directory and its BestBall subfolder. If not found, search one level of subdirectories.
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(directoryPath, $"best_ball_{season}_week_*.json", SearchOption.TopDirectoryOnly));
        var bestBallSub = Path.Combine(directoryPath, "BestBall");
        if (Directory.Exists(bestBallSub))
            files.AddRange(Directory.GetFiles(bestBallSub, $"best_ball_{season}_week_*.json", SearchOption.TopDirectoryOnly));

        if (files.Count == 0)
        {
            foreach (var sub in Directory.GetDirectories(directoryPath))
            {
                files.AddRange(Directory.GetFiles(sub, $"best_ball_{season}_week_*.json", SearchOption.TopDirectoryOnly));
            }
        }

        return files.Select(f =>
        {
            var match = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(f), $@"best_ball_{season}_week_(\d+)\.json");
            return match.Success ? int.Parse(match.Groups[1].Value) : (int?)null;
        })
        .Where(w => w.HasValue)
        .Select(w => w!.Value)
        .Distinct()
        .OrderBy(w => w)
        .ToList();
    }

    public async Task WriteWeeklySnapshotAsync(string path, WeeklyLeagueSnapshot snapshot)
    {
        await YahooFantasyPersistence.WriteJsonFileAsync(path, snapshot);
    }
}