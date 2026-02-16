import { useState } from "react";
import { SeasonBestBallTeam, SeasonBestBallPlayer } from "../models/League";
import { toOrdinal, getRankColor, getRankHighlight } from "../util/Helpers";
import { ArrowToggle } from "./ArrowToggle";
import { getPlayerImage } from "../services/playerImageService";

interface Props {
  team: SeasonBestBallTeam;
  rank: number;
}

export default function SeasonBestBallCard({ team, rank }: Props) {
  const [open, setOpen] = useState(false);
  const toggleOpen = () => setOpen(!open);

  const fmt1 = (num?: number) => (num ?? 0).toFixed(1);
  const fmt2 = (num?: number) => (num ?? 0).toFixed(2);
  const fmtPercent = (num?: number) => ((num ?? 0) * 100).toFixed(1) + "%";

  const mvp: SeasonBestBallPlayer | undefined =
    team.Players?.length > 0
      ? team.Players.reduce((prev, curr) =>
          (curr.TotalContributedPoints ?? 0) > (prev.TotalContributedPoints ?? 0)
            ? curr
            : prev
        )
      : undefined;

  return (
    <div
      className={`${getRankHighlight(rank)} mb-1 rounded-md border overflow-hidden`}
    >
      {/* Header */}
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center pt-[3px] focus:outline-none"
      >
        {/* Left: Rank + Manager */}
        <div className="flex items-center gap-2 pl-2 min-w-0">
          <span className={`font-bold text-lg ${getRankColor(rank)}`}>
            {toOrdinal(rank)}
          </span>
          <span className="font-medium text-[var(--text-primary)] whitespace-nowrap">
            {team.ManagerName ?? "Unknown Manager"}
          </span>
        </div>

        {/* Right: Total Points + MVP immediately before Chevron */}
        <div className="flex items-center gap-2 ml-auto min-w-max">
          <div className="flex flex-col items-end text-xs font-bold text-[var(--text-secondary)] gap-0.5">
            <span className="flex items-center gap-1">
  <span className="text-[var(--text-secondary)]">
    Total Points:
  </span>
  <span className="text-[var(--accent-primary)]">
    {fmt1(team.SeasonTotalBestBallPoints)}
  </span>
</span>
            {mvp && (
              <div className="flex items-center text-xs gap-1">
                 MVP:
                <img
                  src={getPlayerImage(mvp.PlayerKey) || process.env.PUBLIC_URL + "/default-player.png"}
                  alt={mvp.PlayerName}
                  className="w-4 h-6 object-cover relative -top-0.5"
                  onError={(e) =>
                    (e.currentTarget.src = process.env.PUBLIC_URL + "/default-player.png")
                  }
                />
                <span className="whitespace-nowrap">
                 - {fmt1(mvp.TotalContributedPoints)}
                </span>
              </div>
            )}
          </div>

          {/* Chevron */}
          <ArrowToggle open={open} className="mr-2"/>
        </div>
      </button>

      {/* Expanded Player Table */}
      {open && team.Players?.length > 0 && (
        <div className="bg-[var(--bg-active)] text-[var(--text-primary)] text-xs">
          {/* Team summary row */}
          <div className="flex px-2 justify-between py-2 text-[var(--text-primary)] text-[10px] font-semibold">
            <span>Best Week: {fmt1(team.BestWeekScore)}</span>
            <span>Worst Week: {fmt1(team.WorstWeekScore)}</span>
            <span>Average Rank: {fmt2(team.AverageRank)}</span>
          </div>

          <table className="w-full text-left border-t border-[var(--border-primary)]">
            <thead>
              <tr className="text-[var(--accent-secondary)] text-[10px] underline">
                <th className="pl-2 py-1">Player</th>
                <th>FTPS</th>
                <th>% of Pts</th>
                <th className="pr-1 py-1">Wks Started</th>
              </tr>
            </thead>
            <tbody>
              {team.Players.map((p: SeasonBestBallPlayer) => (
                <tr key={p.PlayerKey} className="border-t border-[var(--border-primary)]">
                  <td className="flex items-center">
                    <img
                      src={getPlayerImage(p.PlayerKey) || process.env.PUBLIC_URL + "/default-player.png"}
                      alt={p.PlayerName}
                      className="w-6 h-8 my-1 mx-1 object-cover"
                      onError={(e) =>
                        (e.currentTarget.src = process.env.PUBLIC_URL + "/default-player.png")
                      }
                    />
                    <span>{p.PlayerName ?? "Unknown"}</span>
                  </td>
                  <td>{fmt1(p.TotalContributedPoints)}</td>
                  <td>{fmtPercent(p.ContributionPercent)}</td>
                  <td>
  {fmtPercent(p.WeeksStarted / p.WeeksOnRoster)} {" - "} 
  {p.WeeksStarted ?? 0}/{p.WeeksOnRoster ?? 0}
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
