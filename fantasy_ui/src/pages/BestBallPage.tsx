import { useEffect, useState } from "react";
import {
  BestBallTeam,
  BestBallPlayer,
  SeasonBestBallTeam,
  BestBallContext,
} from "../models/League";
import BestBallTeamCard from "../components/BestBallTeamCard";
import SeasonBestBallCard from "../components/SeasonBestBallCard";
import { ViewToggle } from "../components/ViewToggle";
import { WeekSelector } from "../components/WeekSelector";
import { usePersistentState } from "../hooks/usePersistentState";
import { DelayedLoader } from "../components/DelayedLoader";

// ---- CONFIG ----
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector/Data";

type ViewMode = "WEEKLY" | "SEASON";

export default function BestBallPage() {
  const [teams, setTeams] = useState<BestBallTeam[] | null>(null);
  const [seasonTeams, setSeasonTeams] = useState<SeasonBestBallTeam[] | null>(
    null
  );
  const [season, setSeason] = useState<number | null>(null);
  const [week, setWeek] = useState<number | null>(null);
  const [availableWeeks, setAvailableWeeks] = useState<number[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [viewMode, setViewMode] = usePersistentState<ViewMode>(
    "bestball:viewMode",
    "WEEKLY"
  );

  // -------------------------------
  // Load league context
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

        const mapped: SeasonBestBallTeam[] = data.Teams.map((t: any) => ({
          ...t,
          ManagerName: t.ManagerName,
        }));

        mapped.sort(
          (a, b) => b.SeasonTotalBestBallPoints - a.SeasonTotalBestBallPoints
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
          players: t.Players?.map((p: any) => ({
            playerKey: p.PlayerKey,
            fullName: p.FullName,
            fantasyPoints: p.FantasyPoints,
            bestBallSlot: p.BestBallSlot,
            rawStats: {
              points: p.RawStats?.PTS ?? 0,
              rebounds: p.RawStats?.REB ?? 0,
              assists: p.RawStats?.AST ?? 0,
              steals: p.RawStats?.STL ?? 0,
              blocks: p.RawStats?.BLK ?? 0,
              turnovers: p.RawStats?.TO ?? 0,
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

  // -------------------------------
  // Render
  // -------------------------------
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

          {viewMode === "WEEKLY" && week !== null && (
            <WeekSelector
              week={week}
              availableWeeks={availableWeeks}
              currentWeekIndex={currentWeekIndex}
              setWeek={setWeek}
            />
          )}
        </div>

        {/* Content */}
        <DelayedLoader
          loading={loading}
          dataLoaded={
            (viewMode === "WEEKLY" && teams !== null) ||
            (viewMode === "SEASON" && seasonTeams !== null)
          }
          error={error}
          message={
            viewMode === "SEASON"
              ? "Loading season standings..."
              : "Loading best ball results..."
          }
        >
          {viewMode === "SEASON" ? (
            seasonTeams && seasonTeams.length > 0 ? (
              <div className="flex flex-col gap-1">
                {seasonTeams.map((team, index) => (
                  <SeasonBestBallCard key={team.TeamKey} team={team} rank={index + 1} />
                ))}
              </div>
            ) : (
              <div
                className="flex justify-center items-center h-24"
                style={{ color: "var(--text-secondary)" }}
              >
              </div>
            )
          ) : viewMode === "WEEKLY" ? (
            teams && teams.length > 0 ? (
              teams.map((team, index) => (
                <BestBallTeamCard key={`${team.teamKey}-${week}`} team={team} rank={index + 1} />
              ))
            ) : (
              <div
                className="flex justify-center items-center h-24"
                style={{ color: "var(--text-secondary)" }}
              >
              </div>
            )
          ) : null}
        </DelayedLoader>
      </div>
    </div>
  );
}
