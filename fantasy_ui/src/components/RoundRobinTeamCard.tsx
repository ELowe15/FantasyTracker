import React, { useState } from "react";
import {
  RoundRobinResult,
  RoundRobinMatchup,
  TeamRoundRobinRecord,
} from "../models/League";
import { formatRecord, getPercentageCategory, getRankHighlight, toOrdinal, getRankColor } from "../util/Helpers";
import { ArrowToggle } from "./ArrowToggle";

interface Props {
  result: RoundRobinResult;
  rank: number;
  viewMode: "WEEKLY" | "SEASON";
}

const RoundRobinTeamCard: React.FC<Props> = ({ result, rank, viewMode }) => {
  const [expanded, setExpanded] = useState(false);

  const { Team, TeamRecord, Matchups } = result;

  // Format W/L/T for a single matchup
  const formatScore = (teamScore: number, opponentScore: number) => {
    let letter = "T";
    let color = "text-[var(--accent-warning)]";

    if (teamScore > opponentScore) {
      letter = "W";
      color = "text-[var(--accent-success)]";
    } else if (teamScore < opponentScore) {
      letter = "L";
      color = "text-[var(--accent-error)]";
    }

    return (
      <>
        {teamScore}–{opponentScore}{" "}
        <span className={`font-semibold ${color}`}>{letter}</span>
      </>
    );
  };

  // Format category totals
  const formatCategoryRecord = (
    wins: number,
    losses: number,
    ties: number
  ) => (
    <span className="font-medium">
      <span>{wins}</span>-
      <span>{losses}</span>-
      <span>{ties}</span>
    </span>
  );

  return (
    <div className={`${getRankHighlight(rank)} rounded-md border text-[var(--text-primary)] overflow-x-auto border-[var(--border-primary)]`}>
      {/* Header */}
      <button
        onClick={() => setExpanded((e) => !e)}
        className="w-full flex justify-between items-center p-2 text-left focus:outline-none"
      >
        <div className="flex items-center gap-1 font-semibold text-sm min-w-0">
          {/* Rank with special color */}
          <span className={getRankColor(rank)}>{toOrdinal(rank)}</span>

          {/* Manager name with standard color */}
          <span className="truncate text-[var(--text-primary)]">{Team.ManagerName}</span>
        </div>

        <div className="flex items-center gap-2 text-bold text-sm">
          {formatRecord(TeamRecord.MatchupWins, TeamRecord.MatchupLosses, TeamRecord.MatchupTies)}
          <ArrowToggle open={expanded} />
        </div>
      </button>

      {/* Expandable Content */}
      {expanded && (
        <div className="px-3 pb-3 text-xs border-t border-[var(--border-primary)]">
          {Matchups.length === 0 ? (
            <div className="text-[var(--text-secondary)] italic pt-2">No matchup data</div>
          ) : (
            <div className="flex flex-row gap-3 items-start">

              {/* Left: Matchups */}
              <div className="w-full md:w-1/2">
                <div className="flex justify-between text-[11px] text-[var(--text-secondary)] pt-2 pb-1 border-b border-[var(--border-primary)]">
                  <span>Opponent</span>
                  <span className="text-right">
                    {viewMode === "WEEKLY" ? "Score" : "(W-L-T)"}
                  </span>
                </div>
                <div className="space-y-1 pt-1">
                  {Matchups.map((m: RoundRobinMatchup) => (
                    <div
                      key={m.OpponentTeamKey}
                      className="flex justify-between py-[2px] rounded-sm odd:bg-white/10"
                    >
                      <span className="truncate text-[var(--text-primary)]">
                        {m.ManagerName}
                      </span>
                      <span className="text-right font-medium">
                        {viewMode === "WEEKLY"
                          ? formatScore(
                              m.CategoryWins,
                              m.OpponentCategoryWins
                            )
                          : formatCategoryRecord(
                              m.Wins,
                              m.Losses,
                              m.Ties
                            )}

                      </span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Right: Category Performance */}
              <div className="w-full md:w-1/2 md:mt-0">
                <div className="flex justify-between text-[11px] text-[var(--text-secondary)] pt-2 pb-1 border-b border-[var(--border-primary)]">
                  <span>Category</span>
                  <span className="text-right">(W-L-T)</span>
                </div>
                <div className="space-y-1 pt-1">
                  {Object.values(result.TeamRecord.CategoryRecords).map((cat) => (
                    <div key={cat.Category} className="flex justify-between py-[2px] rounded-sm odd:bg-white/10"
>
                        <span className="text-[var(--text-primary)]">{getPercentageCategory(cat.Category)}</span>
                        <span className="text-right font-medium">
                        <span>{cat.Wins}</span>-
                        <span>{cat.Losses}</span>-
                        <span>{cat.Ties}</span>
                        </span>
                    </div>
                    ))}
                </div>

                {/* Total Category Record */}
                <div className="border-t border-[var(--border-primary)] mt-1 pt-1 flex justify-between font-bold font-medium text-[var(--text-primary)] text-sm">
                  <span>Total</span>
                  {formatCategoryRecord(
                    TeamRecord.CategoryWins,
                    TeamRecord.CategoryLosses,
                    TeamRecord.CategoryTies
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default RoundRobinTeamCard;
