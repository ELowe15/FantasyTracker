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
            ? "text-[var(--accent-primary)]"
            : "text-[var(--text-disabled)] cursor-not-allowed"
        }`}
      >
        &lt;
      </button>

      <select
        value={week ?? ""}
        onChange={(e) => setWeek(Number(e.target.value))}
className="
  flex items-center justify-center
  appearance-none
  bg-[rgba(255,255,255,0.05)]
  text-[var(--accent-primary)]
  text-xs
  font-semibold
  px-3 py-1
  rounded-full
  border border-[var(--accent-primary)]
  w-20
  relative
  transition-all duration-200
  focus:outline-none
"



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
            ? "text-[var(--accent-primary)] "
            : "text-[var(--text-disabled)] cursor-not-allowed"
        }`}
      >
        &gt;
      </button>
    </div>
  );
};
