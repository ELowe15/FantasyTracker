import { useState } from "react";
import { TeamRoster } from "../models/League";

interface Props {
  team: TeamRoster;
}

export default function TeamCard({ team }: Props) {
  const [open, setOpen] = useState(false);

  const toggleOpen = () => setOpen(!open);

  return (
    <div className="bg-slate-800 shadow-lg rounded-xl mb-2 border border-slate-700">
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center p-2 text-left"
      >
        <div>
          <h2 className="text-md text-amber-400 drop-shadow">
            {team.managerName + "'s Team"}
          </h2>
        </div>
        <span
          className={`transform transition-transform ${
            open ? "rotate-90" : "rotate-0"
          } text-cyan-400`}
        >
          ▶
        </span>
      </button>

      {open && (
        <div className="px-4 pb-4">
          <table className="w-full text-sm text-left border-t border-slate-700 mt-1">
            <thead>
              <tr className="text-cyan-400">
                <th className="py-2">Player</th>
                <th>Pos</th>
                <th>Team</th>
                <th>Keep For</th>
              </tr>
            </thead>
            <tbody>
              {team.players.map((p) => (
                <tr
                  key={p.playerKey}
                  className="border-t border-slate-700 hover:bg-slate-700/40"
                >
                  <td className="py-2 text-white">{p.fullName}</td>
                  <td className="text-gray-200">{p.position}</td>
                  <td className="text-gray-200">{p.nbaTeam}</td>
                  <td className="text-amber-300">
                    {p.keeperYears != null ? p.keeperYears +"yrs" : "-"}
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
