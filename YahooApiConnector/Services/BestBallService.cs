using System;
using System.Collections.Generic;
using System.Linq;

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
}
