import React from "react";

interface ViewToggleProps {
  viewMode: "WEEKLY" | "SEASON";
  onChange: (mode: "WEEKLY" | "SEASON") => void;
}

export const ViewToggle: React.FC<ViewToggleProps> = ({ viewMode, onChange }) => {
  return (
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
        {["WEEKLY", "SEASON"].map((mode) => {
          const active = viewMode === mode;

          return (
            <button
              key={mode}
              onClick={() => onChange(mode as "WEEKLY" | "SEASON")}
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
              {mode.charAt(0) + mode.slice(1).toLowerCase()}
            </button>
          );
        })}
      </div>
    </div>
  );
};
