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

        if (!res.ok) {
          throw new Error("Failed to load best ball context");
        }

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

  useEffect(() => {
  if (viewMode !== "SEASON" || !season) return;

  async function loadSeasonData() {
    setLoading(true);
    setError(null);

    try {
      const res = await fetch(
        `${BASE_URL}/season_best_ball_${season}.json`
      );

      if (!res.ok) {
        throw new Error("Failed to load season data");
      }

      const data = await res.json();

      if (!Array.isArray(data.Teams)) {
        throw new Error("Invalid season JSON format");
      }

      const mapped = data.Teams.map((t: any) => {
        let name = t.ManagerName;
        if (name === "evan") name = "EFry";
        if (name === "Evan") name = "ELowe";

        return {
          ...t,
          ManagerName: name,
        } as SeasonBestBallTeam;
      });

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
        const res = await fetch(
          `${BASE_URL}/best_ball_${season}_week_${week}.json`
        );

        if (!res.ok) {
          throw new Error(`Failed to fetch week ${week}`);
        }

        const data = await res.json();

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
  // Derived week navigation state
  // -------------------------------
  const currentWeekIndex = availableWeeks.indexOf(week ?? -1);
  const hasPrevWeek = currentWeekIndex > 0;
  const hasNextWeek =
    currentWeekIndex !== -1 &&
    currentWeekIndex < availableWeeks.length - 1;

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
            {"Standings"}
          </h1>

          {/* View Toggle (always centered) */}
          <div className="flex justify-center">
            <div className="flex rounded-lg overflow-hidden border border-slate-600">
              <button
                onClick={() => setViewMode("WEEKLY")}
                className={`px-5 py-1.5 text-sm ${
                  viewMode === "WEEKLY"
                    ? "bg-cyan-500 text-black"
                    : "bg-slate-800 text-gray-300"
                }`}
              >
                Weekly
              </button>
              <button
                onClick={() => setViewMode("SEASON")}
                className={`px-5 py-1.5 text-sm ${
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
    {/* Previous Week */}
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
      aria-label="Previous week"
    >
      &lt;
    </button>

    {/* Dropdown (smaller) */}
    <select
      value={week}
      onChange={(e) => setWeek(Number(e.target.value))}
      className="
        bg-slate-800
        text-white
        text-xs
        px-2
        py-1
        rounded
        border border-slate-600
        w-18
        text-center
      "
    >
      {availableWeeks.map((w) => (
        <option key={w} value={w}>
          Week {w}
        </option>
      ))}
    </select>

    {/* Next Week */}
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
      aria-label="Next week"
    >
      &gt;
    </button>
  </div>
)}
        </div>

        {/* Content */}
{viewMode === "SEASON" ? (
  <div className="flex flex-col gap-2">
    {seasonTeams?.map((team, index) => {
      const rank = index + 1;
      const ordinal = toOrdinal(rank);

      const rankColor =
        rank === 1
          ? "text-yellow-400"
          : rank === 2
          ? "text-gray-300"
          : rank === 3
          ? "text-orange-400"
          : "text-gray-400";

      const highlight =
        rank === 1
          ? "border-yellow-400 bg-yellow-400/10"
          : rank === 2
          ? "border-gray-300 bg-gray-300/10"
          : rank === 3
          ? "border-orange-400 bg-orange-400/10"
          : "border-slate-700";

      return (
        <div
          key={team.TeamKey}
          className={`flex items-center justify-between rounded-md border px-3 py-2 ${highlight}`}
        >
          {/* Left side */}
          <div className="flex items-center gap-3">
            <span className={`text-xs w-12 ${rankColor}`}>
              {ordinal}
            </span>

            <span className="text-white font-medium">
              {team.ManagerName}
            </span>
          </div>

          {/* Stats (left-aligned column style) */}
          <div className="flex items-center text-xs text-white text-left">
  <span className="w-28">
    Best:&nbsp;
    <span className="text-orange-300">
      {team.BestWeekScore.toFixed(1)}
    </span>
  </span>

  <span className="w-32">
    Worst:&nbsp;
    <span className="text-orange-300">
      {team.WorstWeekScore.toFixed(1)}
    </span>
  </span>

  <span className="w-44 text-white font-semibold text-sm">
    Total Points:&nbsp;
    <span className="text-orange-300">
      {team.SeasonTotalBestBallPoints.toFixed(1)}
    </span>
  </span>
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

function toOrdinal(n: number): string {
  const mod10 = n % 10;
  const mod100 = n % 100;

  if (mod10 === 1 && mod100 !== 11) return `${n}st`;
  if (mod10 === 2 && mod100 !== 12) return `${n}nd`;
  if (mod10 === 3 && mod100 !== 13) return `${n}rd`;
  return `${n}th`;
}

