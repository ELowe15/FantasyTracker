import React, { useState } from "react";
import {
  RoundRobinResult,
  RoundRobinMatchup,
  TeamRoundRobinRecord,
} from "../models/League";
import { formatRecord, getPercentageCategory, getRankHighlight, toOrdinal, getRankColor } from "../util/Helpers";

interface Props {
  result: RoundRobinResult;
  rank: number;
}

const RoundRobinTeamCard: React.FC<Props> = ({ result, rank }) => {
  const [expanded, setExpanded] = useState(false);

  const { Team, TeamRecord, Matchups } = result;

  // Format W/L/T for a single matchup
  const formatScore = (teamScore: number, opponentScore: number) => {
    let letter = "T";
    let color = "text-yellow-400";

    if (teamScore > opponentScore) {
      letter = "W";
      color = "text-green-400";
    } else if (teamScore < opponentScore) {
      letter = "L";
      color = "text-red-400";
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
      <span >{wins}</span>-
      <span >{losses}</span>-
      <span >{ties}</span>
    </span>
  );

  return (
    <div className={`${getRankHighlight(rank)} rounded-md border text-white overflow-x-auto`}>
      {/* Header */}
      <button
        onClick={() => setExpanded((e) => !e)}
        className="w-full flex justify-between items-center p-3 text-left focus:outline-none"
      >
        <div className="flex items-center gap-1 font-semibold text-sm min-w-0">
  {/* Rank with special color */}
  <span className={getRankColor(rank)}>{toOrdinal(rank)}</span>

  {/* Manager name with standard color */}
  <span className="text-white truncate">{Team.ManagerName}</span>
</div>

        <div className="flex items-center gap-2 text-xs">
          {formatRecord(TeamRecord.MatchupWins, TeamRecord.MatchupLosses, TeamRecord.MatchupTies)}
          <span
            className={`transition-transform duration-200 ${
              expanded ? "rotate-180" : ""
            }`}
          >
            ▼
          </span>
        </div>
      </button>

      {/* Expandable Content */}
      {expanded && (
        <div className="px-3 pb-3 text-xs border-t border-slate-700">
          {Matchups.length === 0 ? (
            <div className="text-gray-400 italic pt-2">No matchup data</div>
          ) : (
            <div className="flex flex-col md:flex-row md:space-x-4">
              {/* Left: Matchups */}
              <div className="w-full md:w-1/2">
                <div className="flex justify-between text-[11px] text-gray-400 pt-2 pb-1 border-b border-slate-700">
                  <span>Opponent</span>
                  <span className="text-right">Score</span>
                </div>
                <div className="space-y-1 pt-1">
                  {Matchups.map((m: RoundRobinMatchup) => (
                    <div
                      key={m.OpponentTeamKey}
                      className="flex justify-between"
                    >
                      <span className="truncate">
                        {m.ManagerName}
                      </span>
                      <span className="text-right font-medium">
                        {formatScore(
                          m.CategoryWins,
                          m.OpponentCategoryWins
                        )}
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Right: Category Performance */}
              <div className="w-full md:w-1/2 mt-3 md:mt-0">
                <div className="flex justify-between text-[11px] text-gray-400 pt-2 pb-1 border-b border-slate-700">
                  <span>Category</span>
                  <span className="text-right">(W-L-T)</span>
                </div>
                <div className="space-y-1 pt-1">
                  {Object.values(result.TeamRecord.CategoryRecords).map((cat) => (
                    <div key={cat.Category} className="flex justify-between">
                        <span>{getPercentageCategory(cat.Category)}</span>
                        <span className="text-right font-medium">
                        <span >{cat.Wins}</span>-
                        <span >{cat.Losses}</span>-
                        <span >{cat.Ties}</span>
                        </span>
                    </div>
                    ))}
                </div>

                {/* Total Category Record */}
                <div className="border-t border-slate-700 mt-1 pt-1 flex justify-between font-medium text-gray-300 text-[11px]">
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
