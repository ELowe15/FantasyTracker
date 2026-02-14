const IMAGE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector/Data/player_images.json";

let imageMap: Record<string, string> | null = null;

export async function loadPlayerImages(): Promise<void> {
  if (imageMap) return; // already loaded

  const res = await fetch(IMAGE_URL);
  imageMap = await res.json();
}

export function getPlayerImage(playerKey: string): string | undefined {
  return imageMap ? imageMap[playerKey] : undefined;
}