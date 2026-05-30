# NBA Schedule Feature - Implementation Guide

## Overview

This document describes the new **scalable NBA Schedule feature** added to the YahooApiConnector backend. This feature is designed as a **template pattern** for future backend refactoring.

## Architecture

### Folder Structure

```
YahooApiConnector/
├── NbaSchedule/
│   ├── Models/
│   │   ├── NbaTeam.cs              # NBA team reference data
│   │   ├── NbaGame.cs              # Individual game/matchup
│   │   ├── NbaTeamSchedule.cs      # Season schedule for one team
│   │   └── PlayerGameInfo.cs       # Fantasy player → NBA games link
│   ├── Services/
│   │   ├── YahooNbaScheduleService.cs        # Fetch from Yahoo API
│   │   ├── SchedulePersistenceService.cs     # JSON file I/O
│   │   ├── NbaScheduleService.cs             # Orchestrator
│   │   └── FantasyScheduleEnricherService.cs # Merge roster + schedule
│   ├── Helpers/

│   │   └── DateRangeHelper.cs      # Date/week calculations
│   └── Controllers/
│       └── ScheduleController.cs   # API endpoints template
├── Data/
│   ├── nba/
│   │   ├── 2025/
│   │   │   └── schedule/
│   │   │       ├── season_schedule.json
│   │   │       ├── teams.json
│   │   │       └── games/ (optional daily cache)
│   │   └── 2026/
│   │       └── schedule/
│   ├── yahoo/ (existing, unchanged)
│   └── shared/
└── appsettings.json (updated with DataPaths config)
```

### Design Pattern

**Layers (template for future features):**

1. **Models** — Pure data classes, no logic
   - `NbaGame` — single game
   - `NbaTeamSchedule` — one team's season schedule
   - `PlayerGameInfo` — fantasy player + their NBA team's games

2. **Services** — Business logic
   - **YahooNbaScheduleService** — External API (Yahoo Fantasy)
   - **SchedulePersistenceService** — JSON file I/O
   - **NbaScheduleService** — Orchestrator (coordinates fetch → save)
   - **FantasyScheduleEnricherService** — Consumes Yahoo roster + NBA schedule

3. **Helpers** — Reusable utilities
   - **DateRangeHelper** — Week calculations, season boundaries

4. **Controllers** — API endpoints (template only, not wired yet)
   - Ready for future ASP.NET Core Web API migration

---

## Usage

### Command 1: Fetch NBA Schedule for a Season

Populate the schedule data files from Yahoo Fantasy API:

```bash
dotnet run -- --fetch-nba-schedule 2025
```

**Expected output:**
```
[Program.Main] Fetching NBA schedule for season 2025...
[YahooNbaScheduleService] Fetching week 1...
[YahooNbaScheduleService] Fetching week 2...
...
[SchedulePersistence] Saved 1230 games to Data/nba/2025/schedule/season_schedule.json
✓ Successfully fetched and saved 1230 games for season 2025
```

**Output files:**
- `Data/nba/2025/schedule/season_schedule.json` — All games for the season

---

### Use Case 1: Show Team's Weekly Schedule

Get all games for a specific NBA team in a week:

```csharp
var scheduleService = new NbaScheduleService(yahooNbaService, persistence);
var teamSchedule = await scheduleService.GetTeamScheduleAsync("LAL", season: 2025);

// Get just this week's games
var thisWeekGames = teamSchedule.Games
    .Where(g => g.NbaWeek == 5)
    .ToList();
```

**Output (JSON representation):**
```json
{
  "teamAbbreviation": "LAL",
  "teamName": "Los Angeles Lakers",
  "season": 2025,
  "games": [
    {
      "gameId": "2025-w5-LAL-BOS",
      "homeTeam": "LAL",
      "awayTeam": "BOS",
      "gameTime": "2025-11-15T19:30:00Z",
      "status": "Scheduled",
      "season": 2025,
      "nbaWeek": 5
    }
  ]
}
```

---

### Use Case 2: Show Entire Season Schedule

View all games in the season:

```csharp
var games = await scheduleService.GetSeasonSchedule(season: 2025);
Console.WriteLine($"Total games: {games.Count}");
// Output: Total games: 1230
```

---

### Use Case 3: Show Fantasy Player's Upcoming Games

Given a fantasy roster, show which games each player's NBA team plays:

```csharp
var enricherService = new FantasyScheduleEnricherService(scheduleService);

// Assume fantasyPlayers is your Yahoo roster (List<Player>)
var enriched = await enricherService.EnrichFantasyTeamForDateAsync(
    fantasyPlayers,
    date: DateTime.Parse("2025-11-15"),
    season: 2025
);

// Output: List<PlayerGameInfo>
// Each player + their NBA team's games on that date
```

**Example output:**
```json
[
  {
    "playerKey": "nba.p.123456",
    "playerName": "LeBron James",
    "position": "SF",
    "nbaTeam": "LAL",
    "upcomingGames": [
      {
        "gameId": "2025-w5-LAL-BOS",
        "homeTeam": "LAL",
        "awayTeam": "BOS",
        "gameTime": "2025-11-15T19:30:00Z"
      }
    ],
    "gameCount": 1,
    "playsOnDate": true,
    "opponents": ["BOS"]
  }
]
```

---

### Use Case 4: Analyze Roster Load (Foundation for Future Feature)

Identify how many fantasy roster players have games on a specific date:

```csharp
var loadSummary = await enricherService.GetRosterLoadForDateAsync(
    fantasyPlayers,
    date: DateTime.Parse("2025-11-15"),
    season: 2025
);

Console.WriteLine($"Players with games: {loadSummary.PlayersWithGames}/{loadSummary.TotalRosterPlayers}");
Console.WriteLine($"Playing percentage: {loadSummary.PlayingPercentage:F1}%");
```

**Example output:**
```
Players with games: 8/15
Playing percentage: 53.3%
Games by team: { "LAL": 2, "BOS": 1, "GSW": 2, "MIA": 3 }
```

---

## Data Files

### File: `Data/nba/{season}/schedule/season_schedule.json`

Complete NBA season schedule. Example:

```json
[
  {
    "gameId": "2025-w1-BOS-NYK",
    "homeTeam": "BOS",
    "awayTeam": "NYK",
    "gameTime": "2025-10-24T23:30:00Z",
    "status": "Scheduled",
    "homeScore": null,
    "awayScore": null,
    "season": 2025,
    "nbaWeek": 1
  },
  {
    "gameId": "2025-w1-LAL-GSW",
    "homeTeam": "LAL",
    "awayTeam": "GSW",
    "gameTime": "2025-10-24T02:00:00Z",
    "status": "Scheduled",
    "homeScore": null,
    "awayScore": null,
    "season": 2025,
    "nbaWeek": 1
  }
]
```

**Key fields:**
- `gameTime` — UTC datetime when game starts
- `nbaWeek` — Fantasy week (1-21)
- `status` — "Scheduled", "In Progress", "Final"
- `homeScore` / `awayScore` — Populated after game finishes

---

## Configuration

### File: `appsettings.json`

```json
{
  "DataPaths": {
    "Root": "Data",
    "NbaSubdirectory": "nba",
    "YahooSubdirectory": "yahoo",
    "SharedSubdirectory": "shared"
  }
}
```

Services compute paths dynamically:
```csharp
var scheduleDir = Path.Combine(
  config["DataPaths:Root"],        // "Data"
  config["DataPaths:NbaSubdirectory"],  // "nba"
  season.ToString()                // "2025"
  "schedule"
);
// Result: "Data/nba/2025/schedule"
```

---

## Why This Pattern?

### ✅ Scalable
- Add new seasons without code changes: `Data/nba/2026/schedule/`
- Same services work for any season

### ✅ Reusable
- **DateRangeHelper.cs** used across the app

### ✅ Testable
- Services have no hardcoded paths; all config-driven
- No HTTP or file dependencies in logic layer
- Easy to inject test doubles

### ✅ Database-Ready
- Replace `SchedulePersistenceService` with EF Core DbContext
- Models and service interfaces stay the same
- Controllers ready for Web API layer

### ✅ Clean Separation
- Yahoo data (existing) untouched
- NbaSchedule is self-contained module
- Can disable/remove without affecting other features

---

## Next Steps (Future)

1. **Finish Yahoo API Data Extraction** — Currently extracts matchups; refine to get actual game times
2. **Roster Load Flags** — Create alerts when 3+ players unavailable on a day
3. **Back-to-Back Tracking** — Show when players play consecutive days
4. **HTTP API Integration** — Wire ScheduleController into ASP.NET Core Web API
5. **Database Migration** — Replace JSON with SQL
6. **Multi-league Support** — Allow cross-league schedule views

---

## File Locations

| File | Purpose |
|------|---------|
| `NbaSchedule/Models/` | Data classes |
| `NbaSchedule/Services/` | Business logic |
| `NbaSchedule/Helpers/` | Utilities |
| `NbaSchedule/Controllers/` | API endpoints template |
| `Data/nba/{season}/schedule/` | Schedule JSON files |
| `appsettings.json` | Path configuration |
| `Program.cs` | CLI entry point (`--fetch-nba-schedule`) |

---

## Troubleshooting

**Error: "No games found for week N"**
- Yahoo API may not have schedule data published yet for that week
- Try an earlier week (W1-W10 usually available)

**Error: "Missing Yahoo credentials"**
- Ensure `appsettings.json` has `YahooApi:ClientId`, `ClientSecret`, `RefreshToken`, `LeagueKey`
- Or set env vars: `YAHOO_CLIENT_ID`, `YAHOO_CLIENT_SECRET`, `YAHOO_REFRESH_TOKEN`, `YAHOO_LEAGUE_KEY`

**Data file not found**
- Run `dotnet run -- --fetch-nba-schedule 2025` first to populate files
- Verify `Data/nba/2025/schedule/season_schedule.json` exists

---

## Questions?

Refer to the code comments in each service for detailed explanations of methods and parameters.
