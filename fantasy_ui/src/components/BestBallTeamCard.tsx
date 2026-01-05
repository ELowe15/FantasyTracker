import { useState } from "react";
import { BestBallTeam, BestBallPlayer } from "../models/League";

interface Props {
  team: BestBallTeam;
  rank?: number;
}

// Display order for lineup
const SLOT_ORDER = ["PG", "SG", "SF", "PF", "C", "UTIL", "UTIL", "Bench"];

// Convert 1 → 1st, 2 → 2nd, etc.
function formatOrdinal(n: number) {
  if (n % 100 >= 11 && n % 100 <= 13) return `${n}th`;
  switch (n % 10) {
    case 1:
      return `${n}st`;
    case 2:
      return `${n}nd`;
    case 3:
      return `${n}rd`;
    default:
      return `${n}th`;
  }
}

export default function BestBallTeamCard({ team, rank }: Props) {
  const [open, setOpen] = useState(false);

  const toggleOpen = () => setOpen(!open);

  // Sort players by slot order
  const sortedPlayers = [...team.players].sort((a, b) => {
    const slotA =
      a.bestBallSlot?.startsWith("UTIL") ? "UTIL" : a.bestBallSlot ?? "Bench";
    const slotB =
      b.bestBallSlot?.startsWith("UTIL") ? "UTIL" : b.bestBallSlot ?? "Bench";
    return SLOT_ORDER.indexOf(slotA) - SLOT_ORDER.indexOf(slotB);
  });

  return (
    <div className="bg-slate-800 rounded-lg mb-2 border border-slate-700">
      {/* Compact Header */}
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center px-3 py-2 text-left"
      >
        <div className="flex items-center gap-2 text-sm">
          {rank && (
            <span className="text-amber-400 font-semibold">
              {formatOrdinal(rank)}
            </span>
          )}
          <span className="text-amber-300 font-medium">
            {team.managerName + " -"}
          </span>
          <span className="text-amber-300">
            {team.totalFantasyPoints.toFixed(1)} FPTS
          </span>
        </div>

        <span
          className={`text-cyan-400 text-sm transform transition-transform ${
            open ? "rotate-90" : ""
          }`}
        >
          ▶
        </span>
      </button>

      {/* Expanded Player Table */}
      {open && (
        <div className="px-2 pb-2">
          <table className="w-full text-xs text-left border-t border-slate-700 mt-1">
            <thead>
              <tr className="text-cyan-400 text-[10px]">
                <th className="py-1">POS</th>
                <th>FPTS</th>
                <th>PLAYER</th>
                <th>PTS</th>
                <th>REB</th>
                <th>AST</th>
                <th>STL</th>
                <th>BLK</th>
                <th>TO</th>
              </tr>
            </thead>
            <tbody>
              {sortedPlayers.map((p: BestBallPlayer) => {
                const rawSlot =
                  p.bestBallSlot?.startsWith("UTIL") ? "UTIL" : p.bestBallSlot;
                const isBench = rawSlot === "Bench";

                return (
                  <tr
                    key={p.playerKey}
                    className={`border-t border-slate-700 ${
                      isBench ? "bg-slate-900" : ""
                    }`}
                  >
                    <td className="py-1 text-cyan-400 font-semibold">
                      {rawSlot === "Bench" ? "BN" : rawSlot}
                    </td>
                    <td className="text-amber-300">
                      {p.fantasyPoints.toFixed(1)}
                    </td>
                    <td className="text-white truncate max-w-[110px]">
                      {p.fullName}
                    </td>
                    <td className="text-white">{p.rawStats.points}</td>
                    <td className="text-white">{p.rawStats.rebounds}</td>
                    <td className="text-white">{p.rawStats.assists}</td>
                    <td className="text-white">{p.rawStats.steals}</td>
                    <td className="text-white">{p.rawStats.blocks}</td>
                    <td className="text-white">{p.rawStats.turnovers}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
