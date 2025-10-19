import { useState } from "react";
import { DraftPick } from "../models/League";  
import { TeamGroup } from "../models/League";

interface Props {
  team: TeamGroup;
}

export default function TeamCard({ team }: Props) {
  const [open, setOpen] = useState(false);

  const toggleOpen = () => setOpen(!open);

  return (
    <div className="bg-white shadow-md rounded-xl mb-4 border border-gray-200">
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center p-4 text-left"
      >
        <div>
          <h2 className="text-lg font-bold text-gray-800">{team.manager_name + "'s Team"}</h2>
        </div>
        <span
          className={`transform transition-transform ${
            open ? "rotate-90" : "rotate-0"
          } text-gray-500`}
        >
          ▶
        </span>
      </button>

      {open && (
        <div className="px-4 pb-4">
          <table className="w-full text-sm text-left border-t border-gray-100 mt-2">
            <thead>
              <tr className="text-gray-600">
                <th className="py-2">Player</th>
                <th>Pos</th>
                <th>Round</th>
                <th>Pick</th>
                <th>Years you can keep</th>
              </tr>
            </thead>
            <tbody>
              {team.picks.map((pick) => {
                const keeper = pick.round === 1 ? "0" : "2";
                return (
                  <tr
                    key={pick.player_key + pick.round}
                    className="border-t border-gray-100 hover:bg-gray-50"
                  >
                    <td className="py-2">{pick.player_name}</td>
                    <td>{pick.position}</td>
                    <td>{pick.round}</td>
                    <td>{pick.pick}</td>
                    <td>{keeper}</td>
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