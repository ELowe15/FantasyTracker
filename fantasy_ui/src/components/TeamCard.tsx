import { useState } from "react";
import { TeamRoster } from "../models/League";
import { getKeeperColor } from "../util/Helpers";
import { ArrowToggle } from "./ArrowToggle";
import { getPlayerImage } from "../services/playerImageService";

interface Props {
  team: TeamRoster;
}

export default function TeamCard({ team }: Props) {
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
            {team.managerName + "'s Team"}
          </h2>
        </div>
        <ArrowToggle open={open} className="m-2"/>
      </button>

      {open && (
        <div className="">
          <table
            className="w-full text-sm text-left border-t"
            style={{ borderColor: "var(--text-divider)" }}
          >
            <thead>
              <tr style={{ color: "var(--accent-secondary)" }}>
                <th className="py-2 px-2">Player</th>
                <th>Pos</th>
                <th>Team</th>
                <th>Can Keep</th>
              </tr>
            </thead>
            <tbody>
              {team.players.map((p, index) => (
                <tr
                  key={p.playerKey}
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
    src={getPlayerImage(p.playerKey) || process.env.PUBLIC_URL + "/default-player.png"}
    alt={p.fullName}
    className="w-6 h-6 full object-cover my-1"
    onError={(e) =>
      (e.currentTarget.src = process.env.PUBLIC_URL +"/default-player.png")
    }
  />
  {p.fullName}
</td>

                  <td style={{ color: "var(--text-secondary)" }}>{p.position}</td>
                  <td style={{ color: "var(--text-secondary)" }}>{p.nbaTeam}</td>
                  <td className={`${getKeeperColor(Number(p.keeperYears))}`}>
                    {p.keeperYears != null ? p.keeperYears + "yrs" : "-"}
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
