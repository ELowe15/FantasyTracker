import { useState } from "react";
import { BestBallTeam, BestBallPlayer } from "../models/League";

interface Props {
  team: BestBallTeam;
  rank?: number;
}

// Display order for lineup
const SLOT_ORDER = ["PG", "SG", "SF", "PF", "C", "UTIL", "UTIL", "Bench"];

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
    <div className="bg-slate-800 shadow-lg rounded-xl mb-4 border border-slate-700">
      {/* Header */}
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center p-4 text-left"
      >
        <div>
          <h2 className="text-lg font-bold text-amber-400 drop-shadow">
            {rank ? `${rank}. ` : ""}
            {team.managerName + "'s Best Ball Team"}
          </h2>
          <p className="text-gray-200 text-sm">
            Total Points: {team.totalFantasyPoints.toFixed(1)}
          </p>
        </div>
        <span
          className={`transform transition-transform text-cyan-400 ${
            open ? "rotate-90" : "rotate-0"
          }`}
        >
          ▶
        </span>
      </button>

      {/* Expanded Player Table */}
      {open && (
        <div className="px-4 pb-4">
          <table className="w-full text-sm text-left border-t border-slate-700 mt-2">
            <thead>
              <tr className="text-cyan-400">
                <th className="py-2">POS</th>
                <th>FPTS</th>
                <th>Player</th>
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
                // Normalize UTIL1/UTIL2 to UTIL
                const rawSlot =
                  p.bestBallSlot?.startsWith("UTIL") ? "UTIL" : p.bestBallSlot;
                const isBench = rawSlot === "Bench";

                return (
                  <tr
                    key={p.playerKey}
                    className={`border-t border-slate-700 hover:bg-slate-700/40 ${
                      isBench ? "bg-slate-900" : ""
                    }`}
                  >
                    <td className="py-2 text-cyan-400 font-semibold">
                      {rawSlot ?? "-"}
                    </td>
                    <td className="text-amber-300">{p.fantasyPoints.toFixed(1)}</td>
                    <td className="text-white">{p.fullName}</td>
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
