import { useEffect, useMemo, useState } from "react";
import {
  WeeklyStatsSnapshot,
  RoundRobinResult,
  TeamWeeklyStats,
} from "../models/League";
import RoundRobinTeamCard from "../components/RoundRobinTeamCard";
import { getPoints } from "../util/Helpers";
import SortableStatsTable from "../components/SortableStatsTable";
import { ViewToggle } from "../components/ViewToggle";
import { WeekSelector } from "../components/WeekSelector";

// ---- CONFIG ----
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector/Data";

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
     Load Season Data
  ========================= */
 const [seasonStats, setSeasonStats] = useState<TeamWeeklyStats[]>([]);

useEffect(() => {
  if (!season || viewMode !== "SEASON") return;

  async function loadSeasonData() {
    setLoading(true);
    setError(null);

    try {
      // 1️⃣ Load existing Round Robin season results
      const rrRes = await fetch(`${BASE_URL}/season_round_robin_${season}.json`);
      const rrData: WeeklyStatsSnapshot = await rrRes.json();
      setRoundRobinResults(rrData.RoundRobinResults);
      setTeams(rrData.RoundRobinResults.map(r => r.Team));

      // 2️⃣ Load raw season stats totals
      try {
        const statsRes = await fetch(`${BASE_URL}/season_stats.json`);
        const statsData: {
          TeamKey: string;
          ManagerName: string;
          StatValues: Record<string, string>;
        }[] = await statsRes.json();

        const mappedSeasonStats: TeamWeeklyStats[] = statsData.map(t => ({
          TeamKey: t.TeamKey,
          ManagerName: t.ManagerName,
          StatValues: t.StatValues,
        }));

        setSeasonStats(mappedSeasonStats); // store separately
      } catch {
        console.warn("Failed to load season stats totals");
      }
    } catch {
      setError("Failed to load season Round Robin data");
    } finally {
      setLoading(false);
    }
  }

  loadSeasonData();
}, [season, viewMode]);


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
  if (error)
    return <div className="text-[var(--accent-error)]">{error}</div>;
  if (loading)
    return <div className="text-[var(--text-secondary)]">Loading…</div>;

return (
  <div className="min-h-screen bg-[var(--bg-primary)] px-4 pt-3">
    {/* Header */}
    <div className="flex flex-col gap-2 mb-3">
      <h1 className="text-2xl font-bold text-center text-[var(--text-primary)]">
        Round Robin Standings
      </h1>

      <ViewToggle viewMode={viewMode} onChange={setViewMode} />

      {viewMode === "WEEKLY" && (
        <WeekSelector
          week={week}
          availableWeeks={availableWeeks}
          currentWeekIndex={currentWeekIndex}
          setWeek={setWeek}
        />
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
              viewMode={viewMode}
            />
          ))}
        </div>

        {sortedResults.length > 0 && (
          <SortableStatsTable sortedResults={sortedResults} />
        )}
      </>
    ) : (
      <>
        {/* Season Mode */}
        <div className="flex flex-col gap-2 mb-6">
          {sortedResults.map((r, idx) => (
            <RoundRobinTeamCard
              key={r.TeamKey}
              result={r}
              rank={idx + 1}
              viewMode={viewMode}
            />
          ))}
        </div>

        {seasonStats.length > 0 && (
          <SortableStatsTable
            sortedResults={seasonStats.map((team) => ({
              TeamKey: team.TeamKey,
              Team: team,
              TeamRecord: { MatchupWins: 0, MatchupTies: 0, MatchupLosses: 0 },
            }))}
          />
        )}
      </>
    )}
  </div>
);
}