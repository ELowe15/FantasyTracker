import { useEffect, useMemo, useState } from "react";
import {
  WeeklyStatsSnapshot,
  RoundRobinResult,
  TeamWeeklyStats,
} from "../models/League";
import RoundRobinTeamCard from "../components/RoundRobinTeamCard";
import { getPoints } from "../util/Helpers";

// ---- CONFIG ----
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector";

export default function RoundRobinPage() {
  const [teams, setTeams] = useState<TeamWeeklyStats[]>([]);
  const [roundRobinResults, setRoundRobinResults] = useState<RoundRobinResult[]>([]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [season, setSeason] = useState<number | null>(null);
  const [week, setWeek] = useState<number | null>(null);
  const [availableWeeks, setAvailableWeeks] = useState<number[]>([]);

  /* =========================
     Load Context
  ========================= */
  useEffect(() => {
    async function loadContext() {
      try {
        const res = await fetch(`${BASE_URL}/league_context.json`);
        const ctx = await res.json();
        setSeason(ctx.Season);
        setWeek(ctx.CurrentWeek);
        setAvailableWeeks(ctx.AvailableWeeks);
      } catch {
        setError("Failed to load league context");
      }
    }
    loadContext();
  }, []);

  /* =========================
     Load Weekly Data
  ========================= */
  useEffect(() => {
    if (!season || !week) return;

    async function loadData() {
      setLoading(true);
      setError(null);

      try {
        const res = await fetch(
          `${BASE_URL}/round_robin_${season}_week_${week}.json`
        );

        const data: WeeklyStatsSnapshot = await res.json();
        setRoundRobinResults(data.RoundRobinResults);
        setTeams(data.RoundRobinResults.map(r => r.Team));
      } catch {
        setError("Failed to load weekly stats");
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [season, week]);

  /* =========================
     Category Table Sorting
  ========================= */
  const sortedResults = useMemo(() => {
  return [...roundRobinResults].sort((a, b) => {
    const aPoints = getPoints(
      a.TeamRecord.MatchupWins,
      a.TeamRecord.MatchupTies
    );

    const bPoints = getPoints(
      b.TeamRecord.MatchupWins,
      b.TeamRecord.MatchupTies
    );

    // Descending (best first)
    return bPoints - aPoints;
  });
}, [roundRobinResults]);


  /* =========================
     Render
  ========================= */
  if (error) return <div className="text-red-400">{error}</div>;
  if (loading) return <div className="text-gray-400">Loading…</div>;

  return (
    <div className="min-h-screen bg-slate-900 px-4 pt-3">
      <h1 className="text-2xl font-bold text-center text-white mb-3">
        Weekly Round Robin
      </h1>

      {/* Week Selector */}
      <div className="flex justify-center mb-4">
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

      {/* Round Robin Cards */}
      <div className="flex flex-col gap-2 mb-6">
  {sortedResults.map((r, idx) => (
    <RoundRobinTeamCard
      key={r.TeamKey}
      result={r}
      rank={idx + 1}
    />
  ))}
</div>


      {/* Category Totals Table */}
      {sortedResults.length > 0 && (
        <div className="overflow-x-auto bg-slate-800 rounded-md p-2">
          <table className="min-w-full text-white text-xs">
            <thead>
              <tr>
                <th className="px-2 py-1 text-left">Team</th>
                {Object.keys(sortedResults[0].Team.StatValues).map((stat) => (
                  <th key={stat} className="px-2 py-1 text-left">
                    {stat}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {sortedResults.map((team) => (
                <tr key={team.TeamKey}>
                  <td className="px-2 py-1 font-medium">
                    {team.Team.ManagerName}
                  </td>
                  {Object.keys(team.Team.StatValues).map((key) => (
                    <td key={key} className="px-2 py-1">
                      {team.Team.StatValues[key]}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
