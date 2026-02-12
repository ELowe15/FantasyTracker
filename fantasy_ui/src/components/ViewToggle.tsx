import React from "react";

interface ViewToggleProps {
  viewMode: "WEEKLY" | "SEASON";
  onChange: (mode: "WEEKLY" | "SEASON") => void;
}

export const ViewToggle: React.FC<ViewToggleProps> = ({ viewMode, onChange }) => {
  return (
    <div className="flex justify-center">
      <div className="flex rounded-md overflow-hidden border border-[var(--border-primary)] text-[12px]">
        {["WEEKLY", "SEASON"].map((mode) => (
          <button
            key={mode}
            onClick={() => onChange(mode as "WEEKLY" | "SEASON")}
            className={`px-3 py-1 ${
              viewMode === mode
                ? "bg-[var(--accent-primary)] text-[var(--text-button)]"
                : "bg-[var(--bg-secondary)] text-[var(--text-secondary)]"
            }`}
          >
            {mode.charAt(0) + mode.slice(1).toLowerCase()}
          </button>
        ))}
      </div>
    </div>
  );
};
