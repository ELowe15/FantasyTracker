import { useState } from "react";
import { BestBallTeam, BestBallPlayer } from "../models/League";

interface Props {
  team: BestBallTeam;
  rank?: number; // optional if you might not always pass it
}

export default function BestBallTeamCard({ team, rank }: Props) {
  const [open, setOpen] = useState(false);

  const toggleOpen = () => setOpen(!open);

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
                <th className="py-2">Player</th>
                <th>FP</th>
                <th>PTS</th>
                <th>REB</th>
                <th>AST</th>
                <th>STL</th>
                <th>BLK</th>
                <th>TO</th>
                <th>Slot</th>
              </tr>
            </thead>
            <tbody>
              {team.players.map((p: BestBallPlayer) => (
                <tr
                  key={p.playerKey}
                  className="border-t border-slate-700 hover:bg-slate-700/40"
                >
                  <td className="py-2 text-white">{p.fullName}</td>
                  <td className="text-amber-300">{p.fantasyPoints.toFixed(1)}</td>
                  <td className="text-gray-200">{p.rawStats.points}</td>
                  <td className="text-gray-200">{p.rawStats.rebounds}</td>
                  <td className="text-gray-200">{p.rawStats.assists}</td>
                  <td className="text-gray-200">{p.rawStats.steals}</td>
                  <td className="text-gray-200">{p.rawStats.blocks}</td>
                  <td className="text-gray-200">{p.rawStats.turnovers}</td>
                  <td className="text-cyan-400 font-semibold">
                    {p.bestBallSlot ?? "-"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
