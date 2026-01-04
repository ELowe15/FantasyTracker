import { useEffect, useState } from "react";
import { BestBallTeam } from "../models/League";
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

        const mapped: BestBallTeam[] = data.map((t: any) => {
          let displayManagerName = t.ManagerName;
          if (t.ManagerName === "evan") displayManagerName = "EFry";
          if (t.ManagerName === "Evan") displayManagerName = "ELowe";

          return {
            teamKey: t.TeamKey,
            managerName: displayManagerName,
            totalFantasyPoints: t.TotalFantasyPoints,
            players:
              t.Players?.map((p: any) => ({
                playerKey: p.PlayerKey,
                fullName: p.FullName,
                position: p.Position,
                nbaTeam: p.NbaTeam,
                fantasyPoints: p.FantasyPoints,
                counted: p.Counted,
                countedPosition: p.CountedPosition,
              })) ?? [],
          };
        });

        // 🔥 Sort descending by best ball points
        mapped.sort(
          (a, b) => b.totalFantasyPoints - a.totalFantasyPoints
        );

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
          <BestBallTeamCard
            key={team.teamKey}
            team={team}
            rank={index + 1}
          />
        ))}
      </div>
    </div>
  );
}
