using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

public class BestBallService
{
    private readonly string[] _slots =
    {
        "PG",
        "SG",
        "SF",
        "PF",
        "C",
        "UTIL1",
        "UTIL2"
    };

    // ============================================================
    // PUBLIC ENTRY POINT
    // ============================================================
    public void ProcessWeeklyBestBall(List<WeeklyTeamResult> teams)
    {
        foreach (var team in teams)
        {
            AssignBestBallLineup(team.Players);

            team.TotalBestBallPoints = team.Players
                .Where(p => p.BestBallSlot != "Bench")
                .Sum(p => p.FantasyPoints);
        }
    }

    // ============================================================
    // TRUE BEST BALL OPTIMIZATION
    // ============================================================
    private void AssignBestBallLineup(List<WeeklyPlayerStats> players)
    {
        foreach (var p in players)
            p.BestBallSlot = "Bench";

        var eligible = players
            .Where(p => p.IsEligible)
            .OrderByDescending(p => p.FantasyPoints)
            .ToList();

        double bestTotal = 0;
        Dictionary<string, WeeklyPlayerStats> bestAssignment = null;

        BacktrackAssign(
            slotIndex: 0,
            usedPlayers: new HashSet<WeeklyPlayerStats>(),
            currentAssignment: new Dictionary<string, WeeklyPlayerStats>(),
            eligible: eligible,
            ref bestTotal,
            ref bestAssignment
        );

        if (bestAssignment == null)
            return;

        foreach (var kv in bestAssignment)
            kv.Value.BestBallSlot = kv.Key;
    }

    // ============================================================
    // SLOT-BY-SLOT BACKTRACKING (CORE FIX)
    // ============================================================
    private void BacktrackAssign(
    int slotIndex,
    HashSet<WeeklyPlayerStats> usedPlayers,
    Dictionary<string, WeeklyPlayerStats> currentAssignment,
    List<WeeklyPlayerStats> eligible,
    ref double bestTotal,
    ref Dictionary<string, WeeklyPlayerStats> bestAssignment)
{
    if (slotIndex == _slots.Length)
    {
        double total = currentAssignment.Values.Sum(p => p.FantasyPoints);

        if (total > bestTotal)
        {
            bestTotal = total;
            bestAssignment = new Dictionary<string, WeeklyPlayerStats>(currentAssignment);
        }
        return;
    }

    string slot = _slots[slotIndex];

    bool anyEligibleForSlot = eligible.Any(p =>
        !usedPlayers.Contains(p) &&
        (slot.StartsWith("UTIL") || PlayerCanPlaySlot(p, slot))
    );

    // 🔑 CASE 1: No one can fill this slot → leave it blank
    if (!anyEligibleForSlot)
    {
        BacktrackAssign(
            slotIndex + 1,
            usedPlayers,
            currentAssignment,
            eligible,
            ref bestTotal,
            ref bestAssignment
        );
        return;
    }

    // CASE 2: Try all valid players
    foreach (var player in eligible)
    {
        if (usedPlayers.Contains(player))
            continue;

        if (!slot.StartsWith("UTIL") && !PlayerCanPlaySlot(player, slot))
            continue;

        usedPlayers.Add(player);
        currentAssignment[slot] = player;

        BacktrackAssign(
            slotIndex + 1,
            usedPlayers,
            currentAssignment,
            eligible,
            ref bestTotal,
            ref bestAssignment
        );

        usedPlayers.Remove(player);
        currentAssignment.Remove(slot);
    }
}


    // ============================================================
    // POSITION CHECK
    // ============================================================
    private bool PlayerCanPlaySlot(WeeklyPlayerStats player, string slot)
    {
        var positions = player.Position
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return positions.Contains(slot);
    }

   public async Task RebuildSeasonBestBallAsync(
    int season,
    string outputDirectory)
{
    if (string.IsNullOrWhiteSpace(outputDirectory))
        outputDirectory = Directory.GetCurrentDirectory();

    if (!Directory.Exists(outputDirectory))
        return;

    // Load weekly files for the season
    var weeklyFiles = Directory
        .GetFiles(outputDirectory, $"best_ball_{season}_week_*.json")
        .OrderBy(f => f)
        .ToList();

    if (weeklyFiles.Count == 0)
        return;

    // Maps for aggregation
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

        // Sort teams by best ball points for ranking
        var rankedTeams = snapshot.Teams
            .OrderByDescending(t => t.TotalBestBallPoints)
            .ToList();

        for (int i = 0; i < rankedTeams.Count; i++)
        {
            var team = rankedTeams[i];
            var rank = i + 1;

            // Initialize maps
            if (!teamScoreMap.ContainsKey(team.TeamKey))
                teamScoreMap[team.TeamKey] = new List<double>();

            if (!teamRankMap.ContainsKey(team.TeamKey))
                teamRankMap[team.TeamKey] = new TeamRankTracker();

            if (!managerLookup.ContainsKey(team.TeamKey))
                managerLookup[team.TeamKey] = team.ManagerName;

            if (!teamPlayerMap.ContainsKey(team.TeamKey))
                teamPlayerMap[team.TeamKey] = new Dictionary<string, SeasonBestBallPlayer>();

            // Store weekly score
            teamScoreMap[team.TeamKey].Add(team.TotalBestBallPoints);

            // Ranking
            teamRankMap[team.TeamKey].TotalRankPoints += rank;

            // Player-level stats
            foreach (var player in team.Players)
            {
                if (!teamPlayerMap[team.TeamKey].ContainsKey(player.PlayerKey))
                {
                    teamPlayerMap[team.TeamKey][player.PlayerKey] = new SeasonBestBallPlayer
                    {
                        PlayerKey = player.PlayerKey,
                        PlayerName = player.FullName
                    };
                }

                var seasonPlayer = teamPlayerMap[team.TeamKey][player.PlayerKey];

                seasonPlayer.WeeksOnRoster++;

                if (player.BestBallSlot != "Bench")
                {
                    seasonPlayer.WeeksStarted++;
                    seasonPlayer.TotalContributedPoints += player.FantasyPoints;
                }
            }
        }
    }

    // Build season snapshot
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
            AverageRank = scores.Count == 0
                ? 0
                : (double)teamRankMap[teamKey].TotalRankPoints / scores.Count
        };

        // Attach top 5 players
        if (teamPlayerMap.ContainsKey(teamKey))
        {
            foreach (var player in teamPlayerMap[teamKey].Values)
            {
                player.ContributionPercent =
                    team.SeasonTotalBestBallPoints == 0
                        ? 0
                        : player.TotalContributedPoints / team.SeasonTotalBestBallPoints;
            }

            team.Players = teamPlayerMap[teamKey]
                .Values
                .OrderByDescending(p => p.TotalContributedPoints)
                .Take(7) // keep top 7
                .ToList();
        }

        seasonSnapshot.Teams.Add(team);
    }

    // Sort teams by season total points
    seasonSnapshot.Teams = seasonSnapshot.Teams
        .OrderByDescending(t => t.SeasonTotalBestBallPoints)
        .ToList();

    // Save JSON
    var outputPath = Path.Combine(outputDirectory, $"season_best_ball_{season}.json");

    var options = new JsonSerializerOptions { WriteIndented = true };
    var outputJson = JsonSerializer.Serialize(seasonSnapshot, options);
    await File.WriteAllTextAsync(outputPath, outputJson);
}


}

public class TeamRankTracker
{
    public int TotalRankPoints { get; set; }
}

