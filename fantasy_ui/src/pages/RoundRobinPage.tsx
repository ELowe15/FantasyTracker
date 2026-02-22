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
import { usePersistentState } from "../hooks/usePersistentState";
import { DelayedLoader } from "../components/DelayedLoader";
import {
  SeasonRangeControl,
  SeasonFilterMode,
} from "../components/SeasonRangeControl";

/* =========================
   CONFIG
========================= */
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector/Data";

type ViewMode = "WEEKLY" | "SEASON";

/* =========================
   Percentage Formatter
========================= */
function formatPercentage(value: number): string {
  if (!isFinite(value) || isNaN(value)) return ".000";
  return value.toFixed(3).replace(/^0/, "");
}

/* =========================
   Stat Order for Table
========================= */
const STAT_ORDER = [
  "FG%",
  "FT%",
  "3PM",
  "PTS",
  "REB",
  "AST",
  "STL",
  "BLK",
  "TO",
];

export default function RoundRobinPage() {
  const [teams, setTeams] = useState<TeamWeeklyStats[]>([]);
  const [roundRobinResults, setRoundRobinResults] = useState<
    RoundRobinResult[]
  >([]);
  const [seasonStats, setSeasonStats] = useState<TeamWeeklyStats[]>([]);
  const [season, setSeason] = useState<number | null>(null);
  const [week, setWeek] = useState<number | null>(null);
  const [availableWeeks, setAvailableWeeks] = useState<number[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [viewMode, setViewMode] = usePersistentState<ViewMode>(
    "roundrobin:viewMode",
    "WEEKLY"
  );

  /* =========================
     FILTER STATE
  ========================= */
  const [seasonFilterMode, setSeasonFilterMode] =
    useState<SeasonFilterMode>("FULL");
  const [lastXWeeks, setLastXWeeks] = useState(4);
  const [rangeStart, setRangeStart] = useState<number | null>(null);
  const [rangeEnd, setRangeEnd] = useState<number | null>(null);
  const [weeklySnapshots, setWeeklySnapshots] = useState<
    Record<number, WeeklyStatsSnapshot>
  >({});

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
     Load Weekly View
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
        setRoundRobinResults(data.RoundRobinResults ?? []);
        setTeams(data.RoundRobinResults?.map((r) => r.Team) ?? []);
      } catch {
        setError("Failed to load weekly stats");
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [season, week, viewMode]);

  /* =========================
     Load Season FULL Data
  ========================= */
  useEffect(() => {
    if (!season || viewMode !== "SEASON") return;

    async function loadSeasonData() {
      setLoading(true);
      setError(null);

      try {
        const rrRes = await fetch(
          `${BASE_URL}/season_round_robin_${season}.json`
        );
        const rrData: WeeklyStatsSnapshot = await rrRes.json();
        setRoundRobinResults(rrData.RoundRobinResults ?? []);
        setTeams(rrData.RoundRobinResults?.map((r) => r.Team) ?? []);

        const statsRes = await fetch(`${BASE_URL}/season_stats.json`);
        const statsData: {
          TeamKey: string;
          ManagerName: string;
          StatValues: Record<string, string>;
        }[] = await statsRes.json();

        setSeasonStats(
          statsData.map((t) => ({
            TeamKey: t.TeamKey,
            ManagerName: t.ManagerName,
            StatValues: t.StatValues,
          }))
        );
      } catch {
        setError("Failed to load season data");
      } finally {
        setLoading(false);
      }
    }

    loadSeasonData();
  }, [season, viewMode]);

  /* =========================
     Preload Weeks if Needed
  ========================= */
  useEffect(() => {
    if (!season || viewMode !== "SEASON") return;
    if (seasonFilterMode === "FULL") return;

    async function loadWeeks() {
      const snapshots: Record<number, WeeklyStatsSnapshot> = {};

      for (const w of availableWeeks) {
        const res = await fetch(
          `${BASE_URL}/round_robin_${season}_week_${w}.json`
        );
        snapshots[w] = await res.json();
      }

      setWeeklySnapshots(snapshots);
    }

    loadWeeks();
  }, [season, viewMode, seasonFilterMode, availableWeeks]);

  /* =========================
     Aggregation Logic with proper FG%/FT% handling
  ========================= */
  const filteredSeasonStats = useMemo(() => {
    if (seasonFilterMode === "FULL") return seasonStats;

    let weeksToUse: number[] = [];

    if (seasonFilterMode === "LAST_X") {
      weeksToUse = availableWeeks.slice(-lastXWeeks);
    }

    if (seasonFilterMode === "RANGE" && rangeStart !== null && rangeEnd !== null) {
      weeksToUse = availableWeeks.filter((w) => w >= rangeStart && w <= rangeEnd);
    }

    const aggregated: Record<string, TeamWeeklyStats & {
      fgPctTotal: number;
      ftPctTotal: number;
      pctWeekCount: number;
    }> = {};

    weeksToUse.forEach((w) => {
      const snapshot = weeklySnapshots[w];
      if (!snapshot) return;

      snapshot.RoundRobinResults.forEach((r) => {
        const key = r.TeamKey;

        if (!aggregated[key]) {
          aggregated[key] = {
            TeamKey: r.TeamKey,
            ManagerName: r.Team.ManagerName,
            StatValues: {},
            fgPctTotal: 0,
            ftPctTotal: 0,
            pctWeekCount: 0,
          };
        }

        Object.entries(r.Team.StatValues).forEach(([stat, value]) => {
          if (stat === "FG%" || stat === "FT%") return; // skip percentages
          aggregated[key].StatValues[stat] =
            String(Number(aggregated[key].StatValues[stat] ?? 0) + Number(value ?? 0));
        });

        // Accumulate percentages
        aggregated[key].fgPctTotal += Number(r.Team.StatValues["FG%"] ?? 0);
        aggregated[key].ftPctTotal += Number(r.Team.StatValues["FT%"] ?? 0);
        aggregated[key].pctWeekCount += 1;
      });
    });

    // Compute final percentages
    Object.values(aggregated).forEach((team) => {
      if (team.pctWeekCount > 0) {
        team.StatValues["FG%"] = formatPercentage(team.fgPctTotal / team.pctWeekCount);
        team.StatValues["FT%"] = formatPercentage(team.ftPctTotal / team.pctWeekCount);
      } else {
        team.StatValues["FG%"] = ".000";
        team.StatValues["FT%"] = ".000";
      }

      // Reorder StatValues based on STAT_ORDER
      const ordered: Record<string, string> = {};
      STAT_ORDER.forEach((stat) => {
        if (team.StatValues[stat] !== undefined) {
          ordered[stat] = team.StatValues[stat];
        }
      });
      // Include any remaining stats at the end
      Object.keys(team.StatValues).forEach((stat) => {
        if (!ordered[stat]) ordered[stat] = team.StatValues[stat];
      });
      team.StatValues = ordered;
    });

    return Object.values(aggregated);
  }, [
    seasonFilterMode,
    lastXWeeks,
    rangeStart,
    rangeEnd,
    weeklySnapshots,
    seasonStats,
    availableWeeks,
  ]);

  /* =========================
     Sort Standings
  ========================= */
  const sortedResults = useMemo(() => {
    return [...(roundRobinResults ?? [])].sort((a, b) => {
      const aPoints = getPoints(a.TeamRecord.MatchupWins, a.TeamRecord.MatchupTies);
      const bPoints = getPoints(b.TeamRecord.MatchupWins, b.TeamRecord.MatchupTies);
      return bPoints - aPoints;
    });
  }, [roundRobinResults]);

  const currentWeekIndex = availableWeeks.indexOf(week ?? -1);

  /* =========================
     Render
  ========================= */
  return (
    <div className="min-h-screen bg-[var(--bg-primary)] px-4 pt-3">
      <div className="flex flex-col gap-2 mb-3">
        <h1 className="text-2xl font-bold text-center text-[var(--text-primary)]">
          Round Robin Standings
        </h1>

        <ViewToggle viewMode={viewMode} onChange={setViewMode} />

        {viewMode === "WEEKLY" && week !== null && (
          <WeekSelector
            week={week}
            availableWeeks={availableWeeks}
            currentWeekIndex={currentWeekIndex}
            setWeek={setWeek}
          />
        )}
      </div>

      <DelayedLoader
        loading={loading}
        dataLoaded={
          (viewMode === "WEEKLY" && sortedResults.length > 0) ||
          (viewMode === "SEASON" && filteredSeasonStats.length > 0)
        }
        error={error}
        message={
          viewMode === "SEASON"
            ? "Loading season standings..."
            : "Loading weekly results..."
        }
      >
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

            <SeasonRangeControl
              mode={seasonFilterMode}
              setMode={setSeasonFilterMode}
              lastX={lastXWeeks}
              setLastX={setLastXWeeks}
              rangeStart={rangeStart}
              rangeEnd={rangeEnd}
              setRangeStart={setRangeStart}
              setRangeEnd={setRangeEnd}
              availableWeeks={availableWeeks}
            />

            {filteredSeasonStats.length > 0 && (
              <SortableStatsTable
                sortedResults={filteredSeasonStats.map((team) => ({
                  TeamKey: team.TeamKey,
                  Team: team,
                  TeamRecord: { MatchupWins: 0, MatchupTies: 0, MatchupLosses: 0 },
                }))}
              />
            )}
          </>
        )}
      </DelayedLoader>
    </div>
  );
}