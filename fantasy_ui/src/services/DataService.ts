import { DraftPick } from "../models/League";

export async function fetchDraftData(): Promise<DraftPick[]> {
  const BASE_URL =
  "https://raw.githubusercontent.com/ELowe15/FantasyTracker/main/YahooApiConnector/Data";
  
  const res = await fetch(`${BASE_URL}/draft_results.json`);
  //console.log("Fetch status:", res.status);
  const text = await res.text();
  //console.log("Fetch response:", text);
  try {
    return JSON.parse(text);
  } catch (e) {
    console.error("JSON parse error:", e);
    throw e;
  }
}