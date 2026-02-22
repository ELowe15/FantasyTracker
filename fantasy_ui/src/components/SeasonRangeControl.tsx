import { useEffect } from "react";

export type SeasonFilterMode = "FULL" | "LAST_X" | "RANGE";

interface Props {
  mode: SeasonFilterMode;
  setMode: (m: SeasonFilterMode) => void;

  lastX: number;
  setLastX: (n: number) => void;

  rangeStart: number | null;
  rangeEnd: number | null;
  setRangeStart: (n: number | null) => void;
  setRangeEnd: (n: number | null) => void;

  availableWeeks: number[];
}

/* -------------------------------------------------- */
/* REUSABLE WEEK DROPDOWN                            */
/* -------------------------------------------------- */

interface WeekDropdownProps {
  value: number | null;
  onChange: (value: number) => void;
  availableWeeks: number[];
  label?: string;
}

const WeekDropdown: React.FC<WeekDropdownProps> = ({
  value,
  onChange,
  availableWeeks,
  label = "Week",
}) => {
  return (
    <select
      value={value ?? ""}
      onChange={(e) => onChange(Number(e.target.value))}
      className="
        appearance-none
        bg-[rgba(255,255,255,0.05)]
        text-[var(--accent-primary)]
        text-xs
        font-semibold
        px-3 py-1
        rounded-full
        border border-[var(--accent-primary)]
        w-20
        text-center
        transition-all duration-200
        focus:outline-none
      "
    >
      {availableWeeks.map((w) => (
        <option key={w} value={w}>
          {label} {w}
        </option>
      ))}
    </select>
  );
};

/* -------------------------------------------------- */
/* MAIN COMPONENT                                     */
/* -------------------------------------------------- */

export function SeasonRangeControl({
  mode,
  setMode,
  lastX,
  setLastX,
  rangeStart,
  rangeEnd,
  setRangeStart,
  setRangeEnd,
  availableWeeks,
}: Props) {
  const maxWeeks = availableWeeks.length;

  /* -------------------------------------------------- */
  /* AUTO-INITIALIZE RANGE WHEN SELECTED               */
  /* -------------------------------------------------- */

  useEffect(() => {
    if (mode === "RANGE" && availableWeeks.length > 0) {
      if (rangeStart === null || rangeEnd === null) {
        const first = availableWeeks[0];
        const last = availableWeeks[availableWeeks.length - 1];

        setRangeStart(first);
        setRangeEnd(last);
      }
    }
  }, [mode, availableWeeks, rangeStart, rangeEnd, setRangeStart, setRangeEnd]);

  /* -------------------------------------------------- */
  /* PREVENT START > END                               */
  /* -------------------------------------------------- */

  useEffect(() => {
    if (
      rangeStart !== null &&
      rangeEnd !== null &&
      rangeStart > rangeEnd
    ) {
      setRangeEnd(rangeStart);
    }
  }, [rangeStart, rangeEnd, setRangeEnd]);

  return (
    <div className=" flex flex-col gap-2 shadow-md pb-3">

      {/* Mode Buttons */}
      <div className="flex justify-center">
        <div
          className="
            inline-flex
            p-1
            rounded-full
            bg-[rgba(255,255,255,0.05)]
            border border-[rgba(255,255,255,0.12)]
            backdrop-blur-sm
          "
        >
          {(["FULL", "LAST_X", "RANGE"] as SeasonFilterMode[]).map((m) => {
            const active = mode === m;

            const label =
              m === "FULL"
                ? "Season"
                : m === "LAST_X"
                ? "Last X"
                : "Range";

            return (
              <button
                key={m}
                onClick={() => setMode(m)}
                className={`
                  px-3.5 py-1
                  text-sm
                  font-medium
                  rounded-full
                  transition-all duration-200
                  ${
                    active
                      ? `
                        bg-[var(--accent-primary)]
                        text-[var(--text-on-accent)]
                        shadow-[0_4px_14px_rgba(0,0,0,0.3)]
                      `
                      : `
                        text-[var(--text-secondary)]
                      `
                  }
                `}
              >
                {label}
              </button>
            );
          })}
        </div>
      </div>

      {/* LAST X MODE */}
      {mode === "LAST_X" && (
        <div className="flex justify-center items-center gap-2 text-xs font-medium">
          <span className="text-[var(--text-secondary)]">Show last</span>

          <select
            value={lastX}
            onChange={(e) => setLastX(Number(e.target.value))}
            className="
              appearance-none
              bg-[rgba(255,255,255,0.05)]
              text-[var(--accent-primary)]
              text-xs
              font-semibold
              px-3 py-1
              rounded-full
              border border-[var(--accent-primary)]
              w-12
              text-center
              transition-all duration-200
              focus:outline-none
            "
          >
            {availableWeeks.map((_, index) => {
              const value = index + 1;
              return (
                <option key={value} value={value}>
                  {value}
                </option>
              );
            })}
          </select>

          <span className="text-[var(--text-secondary)]">weeks</span>
        </div>
      )}

      {/* RANGE MODE */}
      {mode === "RANGE" && (
        <div className="flex justify-center items-center gap-2 text-xs font-medium">
          <span className="text-[var(--text-secondary)]">From</span>

          <WeekDropdown
            value={rangeStart}
            onChange={setRangeStart}
            availableWeeks={availableWeeks}
            label="Week"
          />

          <span className="text-[var(--text-secondary)]">to</span>

          <WeekDropdown
            value={rangeEnd}
            onChange={setRangeEnd}
            availableWeeks={availableWeeks}
            label="Week"
          />
        </div>
      )}
    </div>
  );
}

/* -------------------------------------------------- */
/* CORRECT PERCENTAGE FORMATTER                       */
/* -------------------------------------------------- */

export function formatPercentage(value: number): string {
  if (isNaN(value) || !isFinite(value)) return ".000";

  return value.toFixed(3).replace(/^0/, "");
}