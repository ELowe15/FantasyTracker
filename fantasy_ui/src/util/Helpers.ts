export function toOrdinal(n: number): string {
  const mod10 = n % 10;
  const mod100 = n % 100;

  if (mod10 === 1 && mod100 !== 11) return `${n}st`;
  if (mod10 === 2 && mod100 !== 12) return `${n}nd`;
  if (mod10 === 3 && mod100 !== 13) return `${n}rd`;
  return `${n}th`;
}

export function getPoints(w: number, t: number) {
  return w + t * 0.5;
}

export function formatRecord(w: number, l: number, t: number) {
  return `${w}-${l}-${t} (W-L-T)`;
}

export function getPercentageCategory(category: string): string | null {
  switch (category) {
    case "FGM/A":
      return "FG%";
    case "FTM/A":
      return "FT%";
    default:
      return category;
  }
}

/** Returns a text color variable class for top 3 ranks */
export function getRankColor(rank: number = 0) {
  return rank === 1
    ? "text-[var(--text-rank-1)]"
    : rank === 2
    ? "text-[var(--text-rank-2)]"
    : rank === 3
    ? "text-[var(--text-rank-3)]"
    : "text-[var(--text-rank-default)]";
}

export function getRankHighlight(rank: number = 0) {
  return rank === 1
    ? "border-[var(--border-rank-1)] bg-[var(--bg-rank-1)]"
    : rank === 2
    ? "border-[var(--border-rank-2)] bg-[var(--bg-rank-2)]"
    : rank === 3
    ? "border-[var(--border-rank-3)] bg-[var(--bg-rank-3)]"
    : "border-[var(--border-primary)] bg-[var(--bg-card)]";
}

export function getKeeperColor(keeperYears: number) {
  switch (keeperYears) {
    case 2:
      return "text-[var(--accent-success)]";
    case 1:
      return "text-[var(--text-warning)]";
    case 0:
      return "text-[var(--accent-error)]";
    default:
      return "text-[var(--text-primary)]";
  }
}
