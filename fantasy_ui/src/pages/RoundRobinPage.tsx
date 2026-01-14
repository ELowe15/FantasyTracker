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

type ViewMode = "WEEKLY" | "SEASON";

export default function RoundRobinPage() {
  const [teams, setTeams] = useState<TeamWeeklyStats[]>([]);
  const [roundRobinResults, setRoundRobinResults] = useState<RoundRobinResult[]>([]);
  const [viewMode, setViewMode] = useState<ViewMode>("WEEKLY");

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
    if (!season || !week || viewMode !== "WEEKLY") return;

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
  }, [season, week, viewMode]);

    /* =========================
     Load Weekly Data
  ========================= */
  useEffect(() => {
    if (!season || !week || viewMode !== "SEASON") return;

    async function loadData() {
      setLoading(true);
      setError(null);

      try {
        const res = await fetch(
          `${BASE_URL}/season_round_robin_${season}.json`
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
  }, [season, week, viewMode]);

  /* =========================
     Week Navigation State
  ========================= */
  const currentWeekIndex = availableWeeks.indexOf(week ?? -1);
  const hasPrevWeek = currentWeekIndex > 0;
  const hasNextWeek =
    currentWeekIndex !== -1 &&
    currentWeekIndex < availableWeeks.length - 1;

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
      {/* Header */}
      <div className="flex flex-col gap-2 mb-3">
        <h1 className="text-2xl font-bold text-center text-white">
          Round Robin Standings
        </h1>

        {/* View Toggle */}
        <div className="flex justify-center">
          <div className="flex rounded-md overflow-hidden border border-slate-600 text-[12px]">
            <button
              onClick={() => setViewMode("WEEKLY")}
              className={`px-3 py-1 ${
                viewMode === "WEEKLY"
                  ? "bg-cyan-500 text-black"
                  : "bg-slate-800 text-gray-300"
              }`}
            >
              Weekly
            </button>
            <button
              onClick={() => setViewMode("SEASON")}
              className={`px-3 py-1 ${
                viewMode === "SEASON"
                  ? "bg-cyan-500 text-black"
                  : "bg-slate-800 text-gray-300"
              }`}
            >
              Season
            </button>
          </div>
        </div>

        {viewMode === "WEEKLY" && (
          <div className="flex justify-center items-center gap-1">
            <button
              disabled={!hasPrevWeek}
              onClick={() =>
                hasPrevWeek &&
                setWeek(availableWeeks[currentWeekIndex - 1])
              }
              className={`text-lg px-1 ${
                hasPrevWeek
                  ? "text-white hover:text-cyan-400"
                  : "text-gray-600 cursor-not-allowed"
              }`}
            >
              &lt;
            </button>

            <select
              value={week ?? ""}
              onChange={(e) => setWeek(Number(e.target.value))}
              className="bg-slate-800 text-white text-xs px-2 py-1 rounded border border-slate-600 w-18 text-center"
            >
              {availableWeeks.map((w) => (
                <option key={w} value={w}>
                  Week {w}
                </option>
              ))}
            </select>

            <button
              disabled={!hasNextWeek}
              onClick={() =>
                hasNextWeek &&
                setWeek(availableWeeks[currentWeekIndex + 1])
              }
              className={`text-lg px-1 ${
                hasNextWeek
                  ? "text-white hover:text-cyan-400"
                  : "text-gray-600 cursor-not-allowed"
              }`}
            >
              &gt;
            </button>
          </div>
        )}
      </div>

      {/* Content */}
      {viewMode === "WEEKLY" ? (
        <>
          <div className="flex flex-col gap-2 mb-6">
            {sortedResults.map((r, idx) => (
              <RoundRobinTeamCard
                key={r.TeamKey}
                result={r}
                rank={idx + 1}
              />
            ))}
          </div>

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
        </>
      ) : (
        <div className="flex flex-col gap-2 mb-6">
            {sortedResults.map((r, idx) => (
              <RoundRobinTeamCard
                key={r.TeamKey}
                result={r}
                rank={idx + 1}
              />
            ))}
          </div>
      )}
    </div>
  );
}
