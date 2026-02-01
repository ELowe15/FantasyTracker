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

    var weeklyFiles = Directory
        .GetFiles(outputDirectory, $"best_ball_{season}_week_*.json")
        .OrderBy(f => f)
        .ToList();

    if (weeklyFiles.Count == 0)
        return;


    var teamWeekMap = new Dictionary<string, List<(int Week, double Score)>>();
    var teamNameMap = new Dictionary<string, string>();
    var weeksIncluded = new HashSet<int>();

    foreach (var file in weeklyFiles)
    {
        var json = await File.ReadAllTextAsync(file);
        var snapshot = JsonSerializer.Deserialize<WeeklyLeagueSnapshot>(json);

        if (snapshot == null || snapshot.Teams == null)
            continue;

        weeksIncluded.Add(snapshot.Week);

        foreach (var team in snapshot.Teams)
        {
            if (!teamWeekMap.ContainsKey(team.TeamKey))
                teamWeekMap[team.TeamKey] = new List<(int Week, double Score)>();
                
            teamWeekMap[team.TeamKey].Add(
                (snapshot.Week, team.TotalBestBallPoints)
            );


            if (!teamNameMap.ContainsKey(team.TeamKey))
                teamNameMap[team.TeamKey] = team.ManagerName;
        }
    }
    var latestWeek = weeksIncluded.Max();

    var seasonSnapshot = new SeasonBestBallSnapshot
    {
        Season = season,
        LastUpdated = DateTime.UtcNow,
        WeeksIncluded = weeksIncluded.OrderBy(w => w).ToList()
    };

    foreach (var kv in teamWeekMap)
{
    var allWeeks = kv.Value;

    var completedWeeks = allWeeks
        .Where(w => w.Week != latestWeek)
        .Select(w => w.Score)
        .ToList();

    var allScores = allWeeks.Select(w => w.Score).ToList();

    var team = new SeasonBestBallTeam
    {
        TeamKey = kv.Key,
        ManagerName = teamNameMap[kv.Key],

        WeeksPlayed = allScores.Count,
        SeasonTotalBestBallPoints = allScores.Sum(),
        AverageWeeklyBestBallPoints = allScores.Average(),

        // Only compute these if at least 1 completed week exists
        BestWeekScore = completedWeeks.Any()
            ? completedWeeks.Max()
            : 0,

        WorstWeekScore = completedWeeks.Any()
            ? completedWeeks.Min()
            : 0
    };

    seasonSnapshot.Teams.Add(team);
}

    // Sort by season total descending
    seasonSnapshot.Teams = seasonSnapshot.Teams
        .OrderByDescending(t => t.SeasonTotalBestBallPoints)
        .ToList();

    var outputPath = Path.Combine(
        outputDirectory,
        $"season_best_ball_{season}.json"
    );

    var options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    var outputJson = JsonSerializer.Serialize(seasonSnapshot, options);
    await File.WriteAllTextAsync(outputPath, outputJson);
}

}
