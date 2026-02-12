import { useEffect, useState } from "react";
import { fetchDraftData } from "../services/DataService";
import { TeamRoster } from "../models/League";
import TeamRosterCard from "../components/TeamCard";

const DATA_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector/team_results.json";

export default function TeamsPage() {
  const [teams, setTeams] = useState<TeamRoster[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
      try {
        const draftResults = await fetchDraftData();

        const draftedRoundByPlayer: Record<string, number> = {};
        draftResults.forEach((d: any) => {
          draftedRoundByPlayer[d.PlayerKey] = d.round;
        });

        const res = await fetch(DATA_URL);
        if (!res.ok)
          throw new Error(`Failed to fetch team data: ${res.status}`);

        const data = await res.json();

        const mapped: TeamRoster[] = data.map((t: any) => {
          const displayManagerName = t.ManagerName;

          return {
            teamKey: t.TeamKey,
            managerName: displayManagerName,
            players:
              t.Players?.map((p: any) => {
                const round = draftedRoundByPlayer[p.PlayerKey];

                const keeperYears =
                  round === undefined
                    ? 2
                    : round === 1
                    ? 0
                    : 2;

                return {
                  playerKey: p.PlayerKey,
                  fullName: p.FullName,
                  position: p.Position,
                  nbaTeam: p.NbaTeam,
                  keeperYears,
                };
              }) ?? [],
          };
        });

        setTeams(mapped);
      } catch (err) {
        console.error("Error loading team results:", err);
        setError("Failed to load team results. Try again later.");
      }
    }

    loadData();
  }, []);

  if (error) {
    return (
      <div
        className="flex justify-center items-center h-screen"
        style={{ color: "var(--accent-error)" }}
      >
        {error}
      </div>
    );
  }

  if (!teams) {
    return (
      <div
        className="flex justify-center items-center h-screen"
        style={{ color: "var(--text-secondary)" }}
      >
        Loading team results...
      </div>
    );
  }

  return (
    <div className="min-h-screen" style={{ backgroundColor: "var(--bg-app)" }}>
      <div className="max-w-2xl mx-auto p-4">
        <h1
          className="text-3xl font-bold text-center mb-6 drop-shadow-lg tracking-wide"
          style={{ color: "var(--text-primary)" }}
        >
          Fantasy Team Rosters
        </h1>
        {teams.map((team) => (
          <TeamRosterCard key={team.teamKey} team={team} />
        ))}
      </div>
    </div>
  );
}
