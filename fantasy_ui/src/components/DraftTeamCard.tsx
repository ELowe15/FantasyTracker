import { useState } from "react";
import { TeamGroup } from "../models/League";
import { getKeeperColor } from "../util/Helpers";

interface Props {
  team: TeamGroup;
}

export default function DraftTeamCard({ team }: Props) {
  const [open, setOpen] = useState(false);

  const toggleOpen = () => setOpen(!open);

  return (
  <div className="bg-slate-800 shadow-lg rounded-xl mb-2 border border-slate-700">
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center p-2 text-left"
      >
        <div>
          <h2 className="text-md text-amber-400 drop-shadow">{team.manager_name + "'s Team"}</h2>
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
          <table className="w-full text-sm text-left border-t border-slate-700 mt-2">
            <thead>
              <tr className="text-cyan-400">
                <th className="py-2">Player</th>
                <th>Pos</th>
                <th>Round</th>
                <th>Keep For</th>
              </tr>
            </thead>
            <tbody>
              {team.picks.map((pick) => {
                const keeper = pick.round === 1 ? "0" : "2";
                return (
                  <tr
                    key={pick.PlayerKey + pick.round}
                    className="border-t border-slate-700 hover:bg-slate-700/40"
                  >
                    <td className="py-2 text-white">{pick.player_name}</td>
                    <td className="text-gray-200">{pick.position}</td>
                    <td className="text-gray-200">{pick.round}</td>
                    <td className={`${getKeeperColor(Number(keeper))}`}>{keeper + "yrs"}</td>
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