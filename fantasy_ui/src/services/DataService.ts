import { DraftPick } from "../models/League";

export async function fetchDraftData(): Promise<DraftPick[]> {
  const res = await fetch("/data/draft_results.json");
  if (!res.ok) throw new Error("Failed to load draft data");
  return res.json();
}