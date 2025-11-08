import { DraftPick } from "../models/League";

export async function fetchDraftData(): Promise<DraftPick[]> {
  const res = await fetch(`${process.env.PUBLIC_URL}/data/draft_results.json`);
  console.log("Fetch status:", res.status);
  const text = await res.text();
  console.log("Fetch response:", text);
  try {
    return JSON.parse(text);
  } catch (e) {
    console.error("JSON parse error:", e);
    throw e;
  }
}