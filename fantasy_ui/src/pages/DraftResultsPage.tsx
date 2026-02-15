import { useEffect, useState } from "react";
import { fetchDraftData } from "../services/DataService";
import { DraftPick, TeamGroup } from "../models/League";
import DraftTeamCard from "../components/DraftTeamCard";
import { DelayedLoader } from "../components/DelayedLoader";

function groupByTeam(picks: DraftPick[]): TeamGroup[] {
  const teams: { [key: string]: TeamGroup } = {};

  picks.forEach((pick) => {
    const teamKey = pick.TeamKey;

    if (!teams[teamKey]) {
      teams[teamKey] = {
        team_key: teamKey,
        manager_name: pick.manager_name,
        picks: [],
      };
    }

    teams[teamKey].picks.push(pick);
  });

  Object.values(teams).forEach((team) => {
    team.picks.sort((a, b) => a.round - b.round || a.pick - b.pick);
  });

  return Object.values(teams);
}

export default function TeamsPage() {
  const [teams, setTeams] = useState<TeamGroup[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    fetchDraftData()
      .then((picks) => setTeams(groupByTeam(picks)))
      .catch((err) => {
        console.error("Error loading draft data:", err);
        setError("Failed to load draft results");
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div style={{ backgroundColor: "var(--bg-app)", minHeight: "100vh" }}>
      <div className="max-w-2xl mx-auto p-4">
        <h1
          className="text-3xl font-bold text-center mb-6 drop-shadow-lg tracking-wide"
          style={{ color: "var(--text-primary)" }}
        >
          Fantasy Draft Results
        </h1>

        <DelayedLoader
          loading={loading}
          dataLoaded={!!teams}
          error={error}
          message="Loading draft results..."
        >
          {teams?.map((team) => (
            <DraftTeamCard key={team.team_key} team={team} />
          ))}
        </DelayedLoader>
      </div>
    </div>
  );
}
