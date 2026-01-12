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

export const STAT_NAME_MAP: Record<string, string> = {
  "12": "PTS",
  "15": "REB",
  "16": "AST",
  "17": "STL",
  "18": "BLK",
  "19": "TO",
  "10": "3PM",
  "5": "FG%",
  "8": "FT%"
};

// Helper to convert "W-L-T" to a sortable number
export function parseRecord(record?: string) {
  if (!record) return 0; // treat missing records as 0-0-0
  const parts = record.split("-").map(Number);
  return parts[0] * 100 + parts[1] * 10 - parts[2];
}