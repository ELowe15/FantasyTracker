import { useState } from "react";
import { TeamGroup } from "../models/League";
import { getKeeperColor } from "../util/Helpers";
import { ArrowToggle } from "./ArrowToggle";
import { getPlayerImage } from "../services/playerImageService";

interface Props {
  team: TeamGroup;
}

export default function DraftTeamCard({ team }: Props) {
  const [open, setOpen] = useState(false);

  const toggleOpen = () => setOpen(!open);

  return (
    <div
      className="shadow-lg rounded-md mb-2 border overflow-hidden"
      style={{
        backgroundColor: "var(--bg-card)",
        borderColor: "var(--border-primary)",
      }}
    >
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center text-left"
      >
        <div>
          <h2
            className="text-md drop-shadow m-2"
            style={{ color: "var(--text-primary)" }}
          >
            {team.manager_name + "'s Team"}
          </h2>
        </div>
        <ArrowToggle open={open} className="m-2"/>
      </button>

      {open && (
        <div className="">
          <table
            className="w-full text-sm text-left border-t "
            style={{ borderColor: "var(--text-divider)" }}
          >
            <thead>
              <tr style={{ color: "var(--accent-secondary)" }}>
                <th className="py-2 px-2">Player</th>
                <th>Round</th>
                <th className="pr-2">Keep</th>
              </tr>
            </thead>
            <tbody>
              {team.picks.map((pick, index) => {
                const keeper = pick.round === 1 ? "0" : "2";
                return (
                  <tr
                    key={pick.PlayerKey + pick.round}
                    className="border-t hover:bg-opacity-40"
                    style={{
                      borderColor: "var(--text-divider)",
                      backgroundColor:
      index % 2 === 0
        ? "var(--bg-card)"
        : "var(--bg-row-alt)",
                    }}
                  >
                    <td
  className="px-2 flex items-center gap-2"
  style={{ color: "var(--text-primary)" }}
>
  <img
    src={getPlayerImage(pick.PlayerKey) || process.env.PUBLIC_URL + "/default-player.png"}
    alt={pick.player_name}
    className="w-6 h-8 full object-cover"
    onError={(e) =>
      (e.currentTarget.src = process.env.PUBLIC_URL + "/default-player.png")
    }
  />
  <span className="break-words leading-tight">
    {pick.player_name}
  </span>
</td>

                    <td style={{ color: "var(--text-secondary)" }}>{pick.round}</td>
                    <td className={`${getKeeperColor(Number(keeper))}`}>
                      {keeper + "yrs"}
                    </td>
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
