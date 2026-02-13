import { BestBallTeam, BestBallPlayer } from "../models/League";
import { useState, Fragment } from "react";
import { getRankColor, getRankHighlight, toOrdinal } from "../util/Helpers";
import { ArrowToggle } from "./ArrowToggle";

interface Props {
  team: BestBallTeam;
  rank: number;
}

// Display order for lineup
const SLOT_ORDER = ["PG", "SG", "SF", "PF", "C", "UTIL", "UTIL", "Bench"];
const REQUIRED_SLOTS = ["PG", "SG", "SF", "PF", "C"];

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

  // -----------------------------
  // Detect missing required slots
  // -----------------------------
  const assignedSlots = new Set(
    team.players
      .map(p => p.bestBallSlot)
      .filter(Boolean)
      .map(s => (s!.startsWith("UTIL") ? "UTIL" : s))
  );

  const missingSlots = REQUIRED_SLOTS.filter(
    slot => !assignedSlots.has(slot)
  );

  return (
    <div
      className={`${getRankHighlight(rank)} mb-2 rounded-md border text-[var(--text-primary)] overflow-x-auto`}
    >
      {/* Compact Header */}
      <button
        onClick={toggleOpen}
        className="w-full flex justify-between items-center text-left focus:outline-none"
      >
        <div className="flex m-2 items-center gap-2 text-sm">
          {rank && (
            <span className={`${getRankColor(rank)}`}>
              {toOrdinal(rank)}
            </span>
          )}
          <span className="font-medium text-[var(--text-primary)]">
            {team.managerName}
          </span>
        </div>

        {/* Right-aligned stats */}
        <div className="ml-auto flex items-center gap-1">
          <span className="text-[var(--text-primary)] font-bold text-sm">
            {team.totalFantasyPoints.toFixed(1)}
          </span>
          <span className="text-xs font-bold text-[var(--text-secondary)]">
            FTPS 
          </span>
          <ArrowToggle open={open} className="m-2"/>
        </div>
      </button>

      {/* Expanded Player Table */}
      {open && (
        <div className="">
          <table className="bg-[var(--bg-active)] w-full text-xs text-left border-t border-[var(--border-primary)]">
            <thead>
              <tr className="text-[var(--accent-secondary)] text-center text-[10px] m-2">
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
              {/* Active Lineup Divider */}
              <tr>
                <td colSpan={9} className="py-1">
                  <div className="flex items-center gap-3">
                    <div className="flex-1 border-t border-[var(--border-primary)]" />
                    <span className="text-xs uppercase tracking-wider text-[var(--text-secondary)]">
                      Active Lineup
                    </span>
                    <div className="flex-1 border-t border-[var(--border-primary)]" />
                  </div>
                </td>
              </tr>

              {/* Missing Position Rows */}
              {missingSlots.map(slot => (
                <tr
                  key={`missing-${slot}`}
                  className="border-t border-[var(--border-primary)] bg-[var(--bg-error-light)] text-[var(--accent-error)]"
                >
                  <td className="py-1 font-semibold">
                    {slot}
                  </td>
                  <td
                    colSpan={8}
                    className="italic text-xs text-center"
                  >
                    No eligible {slot} on roster
                  </td>
                </tr>
              ))}

              {/* Player Rows */}
              {sortedPlayers.map((p: BestBallPlayer, index) => {
                const rawSlot =
                  p.bestBallSlot?.startsWith("UTIL") ? "UTIL" : p.bestBallSlot;
                const isBench = rawSlot === "Bench";

                const prevPlayer = sortedPlayers[index - 1];
                const prevWasBench =
                  prevPlayer &&
                  (prevPlayer.bestBallSlot === "Bench" ||
                    prevPlayer.bestBallSlot === undefined);

                const showBenchSeparator = isBench && !prevWasBench;

                return (
                  <Fragment key={p.playerKey}>
                    {showBenchSeparator && (
                      <tr>
                        <td colSpan={9} className="py-1 bg-[var(--bg-bench)]">
                          <div className="flex items-center gap-3">
                            <div className="flex-1 border-t border-[var(--border-primary)]" />
                            <span className="text-xs uppercase tracking-wider text-[var(--text-divider)]">
                              Bench
                            </span>
                            <div className="flex-1 border-t border-[var(--border-primary)]" />
                          </div>
                        </td>
                      </tr>
                    )}

                    <tr
                      className={`border-t border-[var(--border-primary)] ${
                        isBench
                          ? "bg-[var(--bg-bench)] text-[var(--text-secondary)]"
                          : "text-[var(--text-primary)] "
                      }`}
                    >
                      <td className="py-1 px-2 text-[var(--accent-secondary)] font-semibold">
                        {isBench ? "BN" : rawSlot}
                      </td>
                      <td className="text-[var(--accent-primary)]">
                        {p.fantasyPoints.toFixed(1)}
                      </td>
                      <td className="truncate max-w-[110px]">
                        {p.fullName}
                      </td>
                      <td>{p.rawStats.points}</td>
                      <td>{p.rawStats.rebounds}</td>
                      <td>{p.rawStats.assists}</td>
                      <td>{p.rawStats.steals}</td>
                      <td>{p.rawStats.blocks}</td>
                      <td>{p.rawStats.turnovers}</td>
                    </tr>
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
