import { useMemo, useState } from "react";

type SortDirection = "asc" | "desc";

interface SortableStatsTableProps {
  sortedResults: any[];
}

export default function SortableStatsTable({
  sortedResults,
}: SortableStatsTableProps) {
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");

  function handleSort(key: string) {
    if (sortKey === key) {
      setSortDirection(prev => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDirection("desc");
    }
  }

  const displayResults = useMemo(() => {
    if (!sortKey) return sortedResults;

    return [...sortedResults].sort((a, b) => {
      const aVal = Number(a.Team.StatValues[sortKey]);
      const bVal = Number(b.Team.StatValues[sortKey]);

      if (isNaN(aVal) || isNaN(bVal)) return 0;

      return sortDirection === "asc" ? aVal - bVal : bVal - aVal;
    });
  }, [sortedResults, sortKey, sortDirection]);

  if (sortedResults.length === 0) return null;

  const statKeys = Object.keys(sortedResults[0].Team.StatValues).filter(
    stat => stat !== "FGM/A" && stat !== "FTM/A"
  );

  return (
    <div className="relative -mx-3 sm:mx-0 overflow-x-auto bg-slate-800 shadow-lg">
      <table className="min-w-full text-white text-[0.70rem] sm:text-sm md:text-base table-auto">
        <thead className="sticky top-0 bg-slate-800 z-10">
          <tr className="border-b border-gray-400">
            <th className="text-center">Team</th>

            {statKeys.map(stat => (
              <th
                key={stat}
                onClick={() => handleSort(stat)}
                className="text-center cursor-pointer select-none"
              >
                <span className="inline-flex items-center">
                  {stat}
                  <SortIndicator
                    active={sortKey === stat}
                    direction={sortDirection}
                  />
                </span>
              </th>
            ))}
          </tr>
        </thead>

        <tbody>
          {displayResults.map(team => (
            <tr
              key={team.TeamKey}
              className="border-b border-gray-700 odd:bg-slate-700 hover:bg-slate-600"
            >
              <td className="font-medium">
                {team.Team.ManagerName}
              </td>

              {statKeys.map(key => (
                <td key={key} className="text-left">
                  {team.Team.StatValues[key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SortIndicator({
  active,
  direction,
}: {
  active: boolean;
  direction: "asc" | "desc";
}) {
  return (
    <span className="inline-flex flex-col justify-center items-center w-1.5 opacity-70">
      {/* Up chevron */}
      <span
        className={`w-1 h-1 border-l border-t border-white rotate-45
          ${!active || direction === "desc" ? "opacity-30" : "opacity-100"}
        `}
      />

      {/* Down chevron */}
      <span
        className={`w-1 h-1 border-l border-b border-white -rotate-45 mt-[-2px]
          ${!active || direction === "asc" ? "opacity-30" : "opacity-100"}
        `}
      />
    </span>
  );
}

