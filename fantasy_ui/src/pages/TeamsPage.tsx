import { useEffect, useState } from "react";
import { fetchDraftData } from "../services/DataService";
import { DraftPick , TeamGroup } from "../models/League";
import TeamCard from "../components/TeamCard";


function groupByTeam(picks: DraftPick[]): TeamGroup[] {
  const teams: { [key: string]: TeamGroup } = {};
  picks.forEach((pick) => {
    if (!teams[pick.team_key]) {
      teams[pick.team_key] = {
        team_key: pick.team_key,
        manager_name: pick.manager_name,
        picks: [],
      };
    }
    teams[pick.team_key].picks.push(pick);
  });
  // Sort picks by round or pick if needed
  Object.values(teams).forEach(team => {
    team.picks.sort((a, b) => a.round - b.round || a.pick - b.pick);
  });
  return Object.values(teams);
}

export default function TeamsPage() {
  const [teams, setTeams] = useState<TeamGroup[] | null>(null);

  useEffect(() => {
    fetchDraftData()
      .then((picks) => setTeams(groupByTeam(picks)))
      .catch((err) => console.error("Error loading draft data:", err));
  }, []);

  if (!teams)
    return (
      <div className="flex justify-center items-center h-screen text-gray-500">
        Loading draft results...
      </div>
    );

  return (
  <div className="min-h-screen bg-slate-900">
      <div className="max-w-2xl mx-auto p-4">
        <h1 className="text-3xl font-bold text-center mb-6 text-white drop-shadow-lg tracking-wide">
          Fantasy Draft Results
        </h1>
        {teams.map((team) => (
          <TeamCard key={team.team_key} team={team} />
        ))}
      </div>
    </div>
  );
}