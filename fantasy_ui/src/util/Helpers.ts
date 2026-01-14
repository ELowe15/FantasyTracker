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