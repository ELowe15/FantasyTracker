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
    <div className="relative -mx-3 sm:mx-0 overflow-x-auto shadow-lg">
      <table className="min-w-full text-[var(--text-primary)] text-[0.70rem] sm:text-sm md:text-base table-auto">
        <thead className="sticky top-0 z-10 bg-[var(--bg-purple-dark)]">
          <tr className="border-b border-[var(--border-primary)]">
            <th className="text-center">Team</th>

            {statKeys.map(stat => (
              <th
                key={stat}
                onClick={() => handleSort(stat)}
                className="text-center cursor-pointer select-none group"
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
          {displayResults.map((team, index) => (
            <tr
              key={team.TeamKey}
              className={`border-b border-[var(--border-primary)]
                ${index % 2 === 0 ? "bg-[var(--bg-purple-light)]" : "bg-[var(--bg-purple-dark)]"}
                hover:bg-[var(--bg-hover)]
              `}
            >
              <td className="font-medium text-[var(--text-primary)]">
                {team.Team.ManagerName}
              </td>

              {statKeys.map(key => (
                <td key={key} className="text-left text-[var(--text-primary)]">
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
    <span className="inline-flex flex-col justify-center items-center w-1.5 group ml-1">
      {/* Up chevron */}
      <span
        className={`
          w-1 h-1 border-l border-t rotate-45
          ${active ? "border-[var(--accent-primary)]" : "border-[var(--text-muted)]"}
          group-hover:border-[var(--accent-primary)]
          ${!active || direction === "desc" ? "opacity-80" : "opacity-100"}
        `}
      />

      {/* Down chevron */}
      <span
        className={`
          w-1 h-1 border-l border-b -rotate-45 mt-[-2px]
          ${active ? "border-[var(--accent-primary)]" : "border-[var(--text-muted)]"}
          group-hover:border-[var(--accent-primary)]
          ${!active || direction === "asc" ? "opacity-80" : "opacity-100"}
        `}
      />
    </span>
  );
}
