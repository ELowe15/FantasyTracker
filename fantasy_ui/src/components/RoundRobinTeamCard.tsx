import React, { useState } from "react";
import { RoundRobinTeam, RoundRobinMatchup } from "../models/League";

interface Props {
  team: RoundRobinTeam & { formattedRecord?: string };
}

const RoundRobinTeamCard: React.FC<Props> = ({ team }) => {
  const [expanded, setExpanded] = useState(false);

  const getResult = (m: RoundRobinMatchup) => {
    if (m.teamScore > m.opponentScore)
      return { label: "Win", className: "text-green-400" };
    if (m.teamScore < m.opponentScore)
      return { label: "Loss", className: "text-red-400" };
    return { label: "Tie", className: "text-yellow-400" };
  };

  return (
    <div className="bg-slate-800 rounded-md text-white">
      {/* Header (always visible / clickable) */}
      <button
        onClick={() => setExpanded((e) => !e)}
        className="w-full flex justify-between items-center p-3 text-left focus:outline-none"
      >
        <div className="font-semibold text-sm">
          {team.rank}. {team.managerName}
        </div>

        <div className="flex items-center gap-2 text-xs text-gray-300">
          <span>
            {team.formattedRecord ??
              `${team.wins}-${team.losses}-${team.ties}`}
          </span>

          {/* Caret */}
          <span
            className={`transition-transform duration-200 ${
              expanded ? "rotate-180" : ""
            }`}
          >
            ▼
          </span>
        </div>
      </button>

      {/* Expandable Matchups */}
      {expanded && (
        <div className="px-3 pb-3 text-xs border-t border-slate-700">
          {team.matchups.length === 0 && (
            <div className="text-gray-400 italic pt-2">
              No matchup data
            </div>
          )}

          {team.matchups.length > 0 && (
            <>
              {/* Column Headers */}
              <div className="flex justify-between text-[11px] text-gray-400 pt-2 pb-1">
                <span className="w-1/2">Opponent</span>
                <span className="w-1/4 text-center">Score</span>
                <span className="w-1/4 text-right">Result</span>
              </div>

              {/* Rows */}
              <div className="space-y-1">
                {team.matchups.map((m: RoundRobinMatchup) => {
                  const result = getResult(m);

                  return (
                    <div
                      key={m.opponentId}
                      className="flex justify-between"
                    >
                      <span className="w-1/2 truncate">
                        {m.opponentName}
                      </span>

                      <span className="w-1/4 text-center">
                        {m.teamScore}–{m.opponentScore}
                      </span>

                      <span
                        className={`w-1/4 text-right font-medium ${result.className}`}
                      >
                        {result.label}
                      </span>
                    </div>
                  );
                })}
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default RoundRobinTeamCard;
