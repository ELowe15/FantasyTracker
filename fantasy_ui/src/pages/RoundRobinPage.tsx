import { useEffect, useMemo, useState } from "react";
import {
  WeeklyTeamStats,
  WeeklyStatsSnapshot,
  TeamRecord,
  RawRoundRobinMatchup,
  RoundRobinTeam,
  RoundRobinMatchup,
} from "../models/League";
import RoundRobinTeamCard from "../components/RoundRobinTeamCard";
import { getPoints, formatRecord, STAT_NAME_MAP } from "../util/Helpers";

// ---- CONFIG ----
const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector";

export default function RoundRobinPage() {
  const [teams, setTeams] = useState<WeeklyTeamStats[]>([]);
  const [records, setRecords] = useState<TeamRecord[]>([]);
  const [matchups, setMatchups] = useState<RawRoundRobinMatchup[]>([]);
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

        const data: WeeklyStatsSnapshot & {
          RoundRobinResults: {
            TeamRecords: TeamRecord[];
            Matchups: RawRoundRobinMatchup[];
          };
        } = await res.json();

        setTeams(data.Teams);
        setRecords(data.RoundRobinResults.TeamRecords);
        setMatchups(data.RoundRobinResults.Matchups);
      } catch (e) {
        console.error(e);
        setError("Failed to load weekly stats");
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [season, week]);

  /* =========================
     Build Round Robin Teams
  ========================= */
  const sortedTeams = useMemo(() => {
  if (teams.length === 0) return [];

  return [...teams].sort((a, b) =>
    a.ManagerName.localeCompare(b.ManagerName)
  );
}, [teams]);

  const roundRobinTeams: RoundRobinTeam[] = useMemo(() => {
    const built: RoundRobinTeam[] = teams.map((team) => {
      const record =
        records.find((r) => r.TeamKey === team.TeamKey) ?? {
          MatchupWins: 0,
          MatchupLosses: 0,
          MatchupTies: 0,
        };

      const teamMatchups: RoundRobinMatchup[] = matchups
        .filter(
          (m) =>
            m.TeamAKey === team.TeamKey ||
            m.TeamBKey === team.TeamKey
        )
        .map((m) => {
          const isTeamA = m.TeamAKey === team.TeamKey;
          const opponentKey = isTeamA ? m.TeamBKey : m.TeamAKey;
          const opponent = teams.find(
            (t) => t.TeamKey === opponentKey
          );

          return {
            opponentId: opponentKey,
            opponentName: opponent?.ManagerName ?? "Unknown",
            teamScore: isTeamA
              ? m.TeamACategoryWins
              : m.TeamBCategoryWins,
            opponentScore: isTeamA
              ? m.TeamBCategoryWins
              : m.TeamACategoryWins,
          };
        });

      return {
        teamId: team.TeamKey,
        managerName: team.ManagerName,
        wins: record.MatchupWins,
        losses: record.MatchupLosses,
        ties: record.MatchupTies ?? 0,
        rank: 0,
        matchups: teamMatchups,
      };
    });

    // Sort + tiebreakers
    built.sort((a, b) => {
      const diff =
        getPoints(b.wins, b.ties) - getPoints(a.wins, a.ties);
      if (diff !== 0) return diff;

      const h2h = a.matchups.find((m) => m.opponentId === b.teamId);
      if (h2h) {
        const d = h2h.teamScore - h2h.opponentScore;
        if (d !== 0) return -d;
      }

      return a.managerName.localeCompare(b.managerName);
    });

    built.forEach((t, i) => (t.rank = i + 1));
    return built;
  }, [teams, records, matchups]);

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
      {roundRobinTeams.map((team) => (
        <RoundRobinTeamCard
          key={team.teamId}
          team={{
            ...team,
            formattedRecord: formatRecord(
              team.wins,
              team.losses,
              team.ties
            ),
          }}
        />
      ))}
    </div>

    {/* Category Totals Table */}
    {sortedTeams.length > 0 && (
      <div className="overflow-x-auto bg-slate-800 rounded-md p-2">
        <table className="min-w-full text-white text-xs">
          <thead>
            <tr>
              <th className="px-2 py-1 text-left">Team</th>
              {Object.keys(sortedTeams[0].StatValues).map((stat) => (
                <th key={stat} className="px-2 py-1 text-left">
                  {STAT_NAME_MAP[stat] || stat}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {sortedTeams.map((team) => (
              <tr key={team.TeamKey}>
                <td className="px-2 py-1 font-medium">
                  {team.ManagerName}
                </td>
                {Object.keys(team.StatValues).map((key) => (
                  <td key={key} className="px-2 py-1">
                    {team.StatValues[key]}
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
