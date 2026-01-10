import { useEffect, useState } from "react";
import { WeeklyTeamStats, WeeklyStatsSnapshot } from "../models/League";

// ---- CONFIG ----
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector";

// Map stat IDs to readable 9-cat names
const STAT_NAME_MAP: Record<string, string> = {
  "12": "PTS",
  "15": "REB",
  "16": "AST",
  "17": "STL",
  "18": "BLK",
  "19": "TO",
  "10": "3PM",
  "5": "FG%",
  "8": "FT%",
};

// Format record for display
function formatRecord(record?: { MatchupWins: number; MatchupLosses: number; MatchupTies?: number }) {
  if (!record) return "0-0-0";
  return `${record.MatchupWins}-${record.MatchupLosses}-${record.MatchupTies ?? 0} (W-L-T)`;
}

// Convert record to numeric value for sorting
function parseRecord(record?: { MatchupWins: number; MatchupLosses: number; MatchupTies?: number }) {
  if (!record) return 0;
  return record.MatchupWins * 100 + record.MatchupLosses * 10 - (record.MatchupTies ?? 0);
}

export default function RoundRobinPage() {
  const [teams, setTeams] = useState<(WeeklyTeamStats & { Record?: any })[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [season, setSeason] = useState<number | null>(null);
  const [week, setWeek] = useState<number | null>(null);
  const [availableWeeks, setAvailableWeeks] = useState<number[]>([]);

  useEffect(() => {
    async function loadContextAndWeeks() {
      try {
        const res = await fetch(`${BASE_URL}/league_context.json`);
        const context = await res.json();
        setSeason(context.Season);
        setAvailableWeeks(context.AvailableWeeks);
        setWeek(context.CurrentWeek);
      } catch {
        setError("Failed to load context");
      }
    }
    loadContextAndWeeks();
  }, []);

  useEffect(() => {
    if (!season || !week) return;

    async function loadWeeklyStats() {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch(`${BASE_URL}/round_robin_${season}_week_${week}.json`);
        const data: WeeklyStatsSnapshot & { RoundRobinResults: { TeamRecords: any[] } } = await res.json();

        // Map TeamRecords by TeamKey for easy lookup
        const recordMap = new Map(data.RoundRobinResults.TeamRecords.map(r => [r.TeamKey, r]));

        // Merge records into teams
        const teamsWithRecords = data.Teams.map(team => ({
          ...team,
          Record: recordMap.get(team.TeamKey) || { MatchupWins: 0, MatchupLosses: 0, MatchupTies: 0 },
        }));

        setTeams(teamsWithRecords);
      } catch (e) {
        console.error(e);
        setError("Failed to load weekly stats.");
      } finally {
        setLoading(false);
      }
    }

    loadWeeklyStats();
  }, [season, week]);

  if (error) return <div className="text-red-400">{error}</div>;
  if (loading || !teams) return <div className="text-gray-400">Loading weekly stats...</div>;

  // Sort leaderboard by matchup record
  const sortedTeams = [...teams].sort((a, b) => parseRecord(b.Record) - parseRecord(a.Record));

  return (
    <div className="min-h-screen bg-slate-900 px-4 pt-3">
      <h1 className="text-2xl font-bold text-center text-white mb-3">
        Weekly Round Robin
      </h1>

      {/* Week Selector */}
      <div className="flex justify-center items-center gap-2 mb-4">
        <select
          value={week ?? ""}
          onChange={(e) => setWeek(Number(e.target.value))}
          className="bg-slate-800 text-white text-xs px-2 py-1 rounded border border-slate-600"
        >
          {availableWeeks.map((w) => (
            <option key={w} value={w}>
              Week {w}
            </option>
          ))}
        </select>
      </div>

      {/* Leaderboard */}
      <div className="flex flex-col gap-1 mb-4 text-sm">
        {sortedTeams.map((team, index) => (
          <div
            key={team.TeamKey}
            className="flex justify-between items-center rounded-md border border-slate-700 px-2 py-1 text-white"
          >
            <div className="flex items-center gap-2 min-w-0 truncate">
              <span className="w-6">{index + 1}.</span>
              <span className="truncate">{team.ManagerName}</span>
            </div>
            <div className="text-xs">{formatRecord(team.Record)}</div>
          </div>
        ))}
      </div>

      {/* Category Totals Table */}
      <div className="overflow-x-auto bg-slate-800 rounded-md p-2">
        <table className="min-w-full text-white text-xs">
          <thead>
            <tr>
              <th className="px-2 py-1 text-left">Team</th>
              {Object.keys(sortedTeams[0].StatValues)
                .filter((id) => id !== "9004003" && id !== "9007006")
                .map((stat) => (
                  <th key={stat} className="px-2 py-1 text-left">
                    {STAT_NAME_MAP[stat] || stat}
                  </th>
                ))}
            </tr>
          </thead>
          <tbody>
            {sortedTeams.map((team) => (
              <tr key={team.TeamKey}>
                <td className="px-2 py-1">{team.ManagerName}</td>
                {Object.keys(team.StatValues)
                  .filter((id) => id !== "9004003" && id !== "9007006")
                  .map((key) => (
                    <td key={key} className="px-2 py-1">
                      {team.StatValues[key]}
                    </td>
                  ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
