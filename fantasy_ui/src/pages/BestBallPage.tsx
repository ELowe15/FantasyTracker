import { useEffect, useState } from "react";
import { BestBallTeam, BestBallPlayer } from "../models/League";
import BestBallTeamCard from "../components/BestBallTeamCard";

// ---- CONFIG ----
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector";

type ViewMode = "WEEKLY" | "SEASON";

// Match backend PascalCase keys
type BestBallContext = {
  Season: number;
  CurrentWeek: number;
  AvailableWeeks: number[];
};

export default function BestBallPage() {
  const [teams, setTeams] = useState<BestBallTeam[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [viewMode, setViewMode] = useState<ViewMode>("WEEKLY");

  const [season, setSeason] = useState<number | null>(null);
  const [week, setWeek] = useState<number | null>(null);
  const [availableWeeks, setAvailableWeeks] = useState<number[]>([]);

  // -------------------------------
  // Load lead context (once)
  // -------------------------------
  useEffect(() => {
    async function loadContextAndWeeks() {
      try {
        const res = await fetch(`${BASE_URL}/league_context.json`);

        if (!res.ok) {
          throw new Error("Failed to load best ball context");
        }

        const context: BestBallContext = await res.json();

        // Validate keys exist
        if (
          !context.Season ||
          !context.CurrentWeek ||
          !Array.isArray(context.AvailableWeeks) ||
          context.AvailableWeeks.length === 0
        ) {
          throw new Error("Invalid best ball context format");
        }

        // Map backend PascalCase to frontend camelCase state
        setSeason(context.Season);
        setAvailableWeeks(context.AvailableWeeks);
        setWeek(context.CurrentWeek); // default to most recent week
      } catch (err) {
        console.error(err);
        setError("No best ball data found");
      }
    }

    loadContextAndWeeks();
  }, []);

  // -------------------------------
  // Load weekly best ball data
  // -------------------------------
  useEffect(() => {
    if (viewMode !== "WEEKLY" || !season || !week) return;

    async function loadWeeklyData() {
      setLoading(true);
      setError(null);

      try {
        const res = await fetch(
          `${BASE_URL}/best_ball_${season}_week_${week}.json`
        );

        if (!res.ok) {
          throw new Error(`Failed to fetch week ${week}`);
        }

        const data = await res.json();

        // Validate Teams array
        if (!data.Teams || !Array.isArray(data.Teams)) {
          throw new Error("Invalid JSON format: 'Teams' array not found");
        }

        const mapped: BestBallTeam[] = data.Teams.map((t: any) => {
          let displayManagerName = t.ManagerName;
          if (t.ManagerName === "evan") displayManagerName = "EFry";
          if (t.ManagerName === "Evan") displayManagerName = "ELowe";

          return {
            teamKey: t.TeamKey,
            managerName: displayManagerName,
            totalFantasyPoints: t.TotalBestBallPoints,
            players:
              t.Players?.map((p: any) => {
                const rawStats = {
                  points: p.RawStats?.["12"] ?? 0,
                  rebounds: p.RawStats?.["15"] ?? 0,
                  assists: p.RawStats?.["16"] ?? 0,
                  steals: p.RawStats?.["17"] ?? 0,
                  blocks: p.RawStats?.["18"] ?? 0,
                  turnovers: p.RawStats?.["19"] ?? 0,
                };

                return {
                  playerKey: p.PlayerKey,
                  fullName: p.FullName,
                  fantasyPoints: p.FantasyPoints,
                  bestBallSlot: p.BestBallSlot,
                  rawStats,
                } as BestBallPlayer;
              }) ?? [],
          } as BestBallTeam;
        });

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
  // UI STATES
  // -------------------------------
  if (error) {
    return (
      <div className="flex justify-center items-center h-screen text-red-400">
        {error}
      </div>
    );
  }

  if (loading || !teams || week === null) {
    return (
      <div className="flex justify-center items-center h-screen text-gray-400">
        Loading best ball results...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-900">
      <div className="max-w-3xl mx-auto p-4">
        {/* Header */}
        <div className="flex flex-col gap-4 mb-6">
          <h1 className="text-3xl font-bold text-center text-white tracking-wide">
            {viewMode === "WEEKLY"
              ? `Week ${week} Best Ball Standings`
              : "Season Best Ball Standings"}
          </h1>

          {/* Controls */}
          <div className="flex justify-center gap-4 flex-wrap">
            {/* View Toggle */}
            <div className="flex rounded-lg overflow-hidden border border-slate-600">
              <button
                onClick={() => setViewMode("WEEKLY")}
                className={`px-4 py-1 text-sm ${
                  viewMode === "WEEKLY"
                    ? "bg-cyan-500 text-black"
                    : "bg-slate-800 text-gray-300"
                }`}
              >
                Weekly
              </button>
              <button
                onClick={() => setViewMode("SEASON")}
                className={`px-4 py-1 text-sm ${
                  viewMode === "SEASON"
                    ? "bg-cyan-500 text-black"
                    : "bg-slate-800 text-gray-300"
                }`}
              >
                Season
              </button>
            </div>

            {/* Week Selector */}
            {viewMode === "WEEKLY" && (
              <select
                value={week}
                onChange={(e) => setWeek(Number(e.target.value))}
                className="bg-slate-800 text-white text-sm px-3 py-1 rounded border border-slate-600"
              >
                {availableWeeks.map((w) => (
                  <option key={w} value={w}>
                    Week {w}
                  </option>
                ))}
              </select>
            )}
          </div>
        </div>

        {/* Content */}
        {viewMode === "SEASON" ? (
          <div className="text-center text-gray-400 mt-12">
            Season-long best ball standings coming soon.
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
