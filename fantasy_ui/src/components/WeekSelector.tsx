import React from "react";

interface WeekSelectorProps {
  week: number | null;
  availableWeeks: number[];
  currentWeekIndex: number;
  setWeek: (week: number) => void;
}

export const WeekSelector: React.FC<WeekSelectorProps> = ({
  week,
  availableWeeks,
  currentWeekIndex,
  setWeek,
}) => {
  const hasPrevWeek = currentWeekIndex > 0;
  const hasNextWeek = currentWeekIndex < availableWeeks.length - 1;

  return (
    <div className="flex justify-center items-center gap-1">
      <button
        disabled={!hasPrevWeek}
        onClick={() => hasPrevWeek && setWeek(availableWeeks[currentWeekIndex - 1])}
        className={`text-lg px-1 ${
          hasPrevWeek
            ? "text-[var(--accent-primary)] hover:text-[var(--accent-secondary)]"
            : "text-[var(--text-secondary)] cursor-not-allowed"
        }`}
      >
        &lt;
      </button>

      <select
        value={week ?? ""}
        onChange={(e) => setWeek(Number(e.target.value))}
        className="bg-[var(--accent-primary)] text-[var(--text-primary)] text-xs px-2 py-1 rounded border border-[var(--border-primary)] w-18 text-center"
      >
        {availableWeeks.map((w) => (
          <option key={w} value={w}>
            Week {w}
          </option>
        ))}
      </select>

      <button
        disabled={!hasNextWeek}
        onClick={() => hasNextWeek && setWeek(availableWeeks[currentWeekIndex + 1])}
        className={`text-lg px-1 ${
          hasNextWeek
            ? "text-[var(--accent-primary)] hover:text-[var(--accent-secondary)]"
            : "text-[var(--text-secondary)] cursor-not-allowed"
        }`}
      >
        &gt;
      </button>
    </div>
  );
};
