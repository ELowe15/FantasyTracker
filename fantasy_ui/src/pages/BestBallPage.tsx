import { useEffect, useState } from "react";
import { BestBallTeam, BestBallPlayer } from "../models/League";
import BestBallTeamCard from "../components/BestBallTeamCard";

const DATA_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector/best_ball_2025_week_11.json";

export default function BestBallPage() {
  const [teams, setTeams] = useState<BestBallTeam[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
      try {
        const res = await fetch(DATA_URL);
        if (!res.ok)
          throw new Error(`Failed to fetch best ball data: ${res.status}`);

        const data = await res.json();

        // ✅ Fix: map over data.Teams, not data
        if (!data.Teams || !Array.isArray(data.Teams)) {
          throw new Error("Invalid JSON format: 'Teams' array not found");
        }

        const mapped: BestBallTeam[] = data.Teams.map((t: any) => {
          let displayManagerName = t.ManagerName;
          if (t.ManagerName === "evan") displayManagerName = "EFry";
          if (t.ManagerName === "Evan") displayManagerName = "ELowe";

          return {
            teamKey: t.TeamKey,
            managerName: displayManagerName,
            totalFantasyPoints: t.TotalBestBallPoints,
            players:
              t.Players?.map((p: any) => {
                const rawStats = {
                  points: p.RawStats?.["12"] ?? 0,
                  rebounds: p.RawStats?.["15"] ?? 0,
                  assists: p.RawStats?.["16"] ?? 0,
                  steals: p.RawStats?.["17"] ?? 0,
                  blocks: p.RawStats?.["18"] ?? 0,
                  turnovers: p.RawStats?.["19"] ?? 0,
                };

                return {
                  playerKey: p.PlayerKey,
                  fullName: p.FullName,
                  fantasyPoints: p.FantasyPoints,
                  bestBallSlot: p.BestBallSlot,
                  rawStats,
                } as BestBallPlayer;
              }) ?? [],
          } as BestBallTeam;
        });

        // Sort by total best ball points descending
        mapped.sort((a, b) => b.totalFantasyPoints - a.totalFantasyPoints);

        setTeams(mapped);
      } catch (err) {
        console.error("Error loading best ball results:", err);
        setError("Failed to load best ball results.");
      }
    }

    loadData();
  }, []);

  if (error) {
    return (
      <div className="flex justify-center items-center h-screen text-red-400">
        {error}
      </div>
    );
  }

  if (!teams) {
    return (
      <div className="flex justify-center items-center h-screen text-gray-400">
        Loading best ball results...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-900">
      <div className="max-w-3xl mx-auto p-4">
        <h1 className="text-3xl font-bold text-center mb-6 text-white tracking-wide">
          Weekly Best Ball Standings
        </h1>

        {teams.map((team, index) => (
          <BestBallTeamCard key={team.teamKey} team={team} rank={index + 1} />
        ))}
      </div>
    </div>
  );
}
