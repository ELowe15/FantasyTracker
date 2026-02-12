import { useEffect, useState } from "react";
import { BestBallTeam, BestBallPlayer } from "../models/League";
import BestBallTeamCard from "../components/BestBallTeamCard";
import { getRankColor, getRankHighlight, toOrdinal } from "../util/Helpers";
import { ViewToggle } from "../components/ViewToggle";
import { WeekSelector } from "../components/WeekSelector";

// ---- CONFIG ----
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector";

type ViewMode = "WEEKLY" | "SEASON";

type BestBallContext = {
  Season: number;
  CurrentWeek: number;
  AvailableWeeks: number[];
};

type SeasonBestBallTeam = {
  TeamKey: string;
  ManagerName: string;
  WeeksPlayed: number;
  SeasonTotalBestBallPoints: number;
  AverageWeeklyBestBallPoints: number;
  BestWeekScore: number;
  WorstWeekScore: number;
};

export default function BestBallPage() {
  const [teams, setTeams] = useState<BestBallTeam[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [viewMode, setViewMode] = useState<ViewMode>("WEEKLY");
  const [season, setSeason] = useState<number | null>(null);
  const [week, setWeek] = useState<number | null>(null);
  const [availableWeeks, setAvailableWeeks] = useState<number[]>([]);
  const [seasonTeams, setSeasonTeams] = useState<SeasonBestBallTeam[] | null>(null);

  // -------------------------------
  // Load league context (once)
  // -------------------------------
  useEffect(() => {
    async function loadContextAndWeeks() {
      try {
        const res = await fetch(`${BASE_URL}/league_context.json`);
        if (!res.ok) throw new Error("Failed to load best ball context");
        const context: BestBallContext = await res.json();

        if (
          !context.Season ||
          !context.CurrentWeek ||
          !Array.isArray(context.AvailableWeeks) ||
          context.AvailableWeeks.length === 0
        ) {
          throw new Error("Invalid best ball context format");
        }

        setSeason(context.Season);
        setAvailableWeeks(context.AvailableWeeks);
        setWeek(context.CurrentWeek);
      } catch (err) {
        console.error(err);
        setError("No best ball data found");
      }
    }

    loadContextAndWeeks();
  }, []);

  // -------------------------------
  // Load season data
  // -------------------------------
  useEffect(() => {
    if (viewMode !== "SEASON" || !season) return;

    async function loadSeasonData() {
      setLoading(true);
      setError(null);

      try {
        const res = await fetch(`${BASE_URL}/season_best_ball_${season}.json`);
        if (!res.ok) throw new Error("Failed to load season data");

        const data = await res.json();
        if (!Array.isArray(data.Teams)) throw new Error("Invalid season JSON format");

        const mapped = data.Teams.map((t: any) => ({
          ...t,
          ManagerName: t.ManagerName,
        } as SeasonBestBallTeam));

        mapped.sort(
          (a: SeasonBestBallTeam, b: SeasonBestBallTeam) =>
            b.SeasonTotalBestBallPoints - a.SeasonTotalBestBallPoints
        );

        setSeasonTeams(mapped);
      } catch (err) {
        console.error(err);
        setError("Failed to load season standings.");
        setSeasonTeams(null);
      } finally {
        setLoading(false);
      }
    }

    loadSeasonData();
  }, [season, viewMode]);

  // -------------------------------
  // Load weekly best ball data
  // -------------------------------
  useEffect(() => {
    if (viewMode !== "WEEKLY" || !season || !week) return;

    async function loadWeeklyData() {
      setLoading(true);
      setError(null);

      try {
        const res = await fetch(`${BASE_URL}/best_ball_${season}_week_${week}.json`);
        if (!res.ok) throw new Error(`Failed to fetch week ${week}`);

        const data = await res.json();
        if (!data.Teams || !Array.isArray(data.Teams)) throw new Error("Invalid JSON format");

        const mapped: BestBallTeam[] = data.Teams.map((t: any) => ({
          teamKey: t.TeamKey,
          managerName: t.ManagerName,
          totalFantasyPoints: t.TotalBestBallPoints,
          players:
            t.Players?.map((p: any) => ({
              playerKey: p.PlayerKey,
              fullName: p.FullName,
              fantasyPoints: p.FantasyPoints,
              bestBallSlot: p.BestBallSlot,
              rawStats: {
                points: p.RawStats?.["PTS"] ?? 0,
                rebounds: p.RawStats?.["REB"] ?? 0,
                assists: p.RawStats?.["AST"] ?? 0,
                steals: p.RawStats?.["STL"] ?? 0,
                blocks: p.RawStats?.["BLK"] ?? 0,
                turnovers: p.RawStats?.["TO"] ?? 0,
              },
            } as BestBallPlayer)) ?? [],
        }));

        mapped.sort((a, b) => b.totalFantasyPoints - a.totalFantasyPoints);
        setTeams(mapped);
      } catch (err) {
        console.error(err);
        setError("Failed to load best ball results.");
        setTeams(null);
      } finally {
        setLoading(false);
      }
    }

    loadWeeklyData();
  }, [season, week, viewMode]);

  // -------------------------------
  // Derived week navigation state
  // -------------------------------
  const currentWeekIndex = availableWeeks.indexOf(week ?? -1);
  const hasPrevWeek = currentWeekIndex > 0;
  const hasNextWeek = currentWeekIndex !== -1 && currentWeekIndex < availableWeeks.length - 1;

  // -------------------------------
  // UI STATES
  // -------------------------------
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

  if (loading || !teams || week === null) {
    return (
      <div
        className="flex justify-center items-center h-screen"
        style={{ color: "var(--text-secondary)" }}
      >
        Loading best ball results...
      </div>
    );
  }

  return (
    <div style={{ backgroundColor: "var(--bg-app)", minHeight: "100vh" }}>
      <div className="max-w-3xl mx-auto px-4 pt-3 pb-4">
        {/* Header */}
        <div className="flex flex-col gap-2 mb-3">
          <h1
            className="text-2xl font-bold text-center"
            style={{ color: "var(--text-primary)" }}
          >
            Best Ball Standings
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
        {viewMode === "SEASON" ? (
          <div className="flex flex-col gap-1">
            {seasonTeams?.map((team, index) => {
              const rank = index + 1;
              const ordinal = toOrdinal(rank);
              const rankColor = getRankColor(rank);
              const highlight = getRankHighlight(rank);

              return (
                <div
                  key={team.TeamKey}
                  className={`flex items-center justify-between rounded-md border px-2 py-1 ${highlight}`}
                >
                  {/* LEFT: Rank + Manager */}
                  <div className="flex items-center min-w-0">
                    <span
                      className={`text-[15px] w-8 ${rankColor}`}
                    >
                      {ordinal}
                    </span>
                    <span
                      className="text-[15px] font-medium truncate"
                      style={{ color: "var(--text-primary)" }}
                    >
                      {team.ManagerName}
                    </span>
                  </div>

                  {/* RIGHT: Stats */}
                  <div
                    className="flex flex-col text-[10px] leading-tight text-right whitespace-nowrap"
                    style={{ color: "var(--text-primary)" }}
                  >
                    <div className="font-bold tracking-tight text-[12px]">
                      Total Points:{" "}
                      <span style={{ color: "var(--accent-primary)" }}>
                        {team.SeasonTotalBestBallPoints.toFixed(1)}
                      </span>
                    </div>
                    <div className="flex justify-end gap-2 mt-1.5" style={{ color: "var(--text-secondary)" }}>
                      <span>
                        Best Wk:{" "}
                        <span style={{ color: "var(--accent-primary)" }}>
                          {team.BestWeekScore.toFixed(1)}
                        </span>
                      </span>
                      <span style={{ color: "var(--text-divider)" }}>|</span>
                      <span>
                        Worst Wk:{" "}
                        <span style={{ color: "var(--accent-primary)" }}>
                          {team.WorstWeekScore.toFixed(1)}
                        </span>
                      </span>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          teams.map((team, index) => (
            <BestBallTeamCard
              key={`${team.teamKey}-${week}`}
              team={team}
              rank={index + 1}
            />
          ))
        )}
      </div>
    </div>
  );
}
