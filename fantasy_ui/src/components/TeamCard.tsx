import { useState } from "react";
import { TeamRoster } from "../models/League";
import { getKeeperColor } from "../util/Helpers";
import { ArrowToggle } from "./ArrowToggle";

interface Props {
  team: TeamRoster;
}

export default function TeamCard({ team }: Props) {
  const [open, setOpen] = useState(false);

  const toggleOpen = () => setOpen(!open);

  return (
    <div
      className="shadow-lg rounded-md mb-2 border"
      style={{
        backgroundColor: "var(--bg-card)",
        borderColor: "var(--border-primary)",
      }}
    >
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center p-2 text-left"
      >
        <div>
          <h2
            className="text-md drop-shadow"
            style={{ color: "var(--text-primary)" }}
          >
            {team.managerName + "'s Team"}
          </h2>
        </div>
        <ArrowToggle open={open} />
      </button>

      {open && (
        <div className="px-4 pb-4">
          <table
            className="w-full text-sm text-left border-t mt-1"
            style={{ borderColor: "var(--text-divider)" }}
          >
            <thead>
              <tr style={{ color: "var(--accent-secondary)" }}>
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
                  className="border-t hover:bg-opacity-40"
                  style={{
                    borderColor: "var(--text-divider)",
                    backgroundColor: "var(--bg-card)",
                  }}
                >
                  <td className="py-2" style={{ color: "var(--text-primary)" }}>
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
