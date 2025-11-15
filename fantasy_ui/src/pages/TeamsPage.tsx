import { useEffect, useState } from "react";
import { TeamRoster } from "../models/League";
import TeamRosterCard from "../components/TeamCard";

const DATA_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/data/team_results.json";

export default function TeamsPage() {
  const [teams, setTeams] = useState<TeamRoster[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch(DATA_URL)
      .then((res) => {
        if (!res.ok) throw new Error(`Failed to fetch data: ${res.status}`);
        return res.json();
      })
      .then((data) => {
        const mapped: TeamRoster[] = data.map((t: any) => ({
          teamKey: t.TeamKey,
          managerName: t.ManagerName,
          players:
            t.Players?.map((p: any) => ({
              playerKey: p.PlayerKey,
              fullName: p.FullName,
              position: p.Position,
              nbaTeam: p.NbaTeam,
              keeperYears: keeperOverrides[p.PlayerKey] ?? undefined,
            })) ?? [],
        }));
        setTeams(mapped);
      })
      .catch((err) => {
        console.error("Error loading team results:", err);
        setError("Failed to load team results. Try again later.");
      });
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
        Loading team results...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-900">
      <div className="max-w-3xl mx-auto p-6">
        <h1 className="text-3xl font-bold text-center mb-8 text-white drop-shadow-lg tracking-wide">
          Fantasy Team Rosters
        </h1>

        {teams.map((team) => (
          <TeamRosterCard key={team.teamKey} team={team} />
        ))}
      </div>
    </div>
  );
}
