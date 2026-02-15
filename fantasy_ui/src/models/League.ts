export interface Player {
  name: string;
  position: string;
  round: number;
}

export interface Team {
  manager: string;
  teamName: string;
  players: Player[];
}

export interface LeagueData {
  teams: Team[];
}

export interface DraftPick {
  round: number;
  pick: number;
  TeamKey: string;
  manager_name: string;
  PlayerKey: string;
  player_name: string;
  position: string;
}

export interface TeamGroup {
  team_key: string;
  manager_name: string;
  picks: DraftPick[];
}

export interface Player {
  playerKey: string;
  fullName: string;
  position: string;
  nbaTeam: string;
  keeperYears?: number;
}

export interface TeamRoster {
  teamKey: string;
  managerName: string;
  players: Player[];
}

export interface BestBallPlayer {
  playerKey: string;
  fullName: string;
  position: string;
  fantasyPoints: number;
  bestBallSlot?: string;
  rawStats: {
    points: number;
    rebounds: number;
    assists: number;
    steals: number;
    blocks: number;
    turnovers: number;
  };
}

export interface BestBallTeam {
  teamKey: string;
  managerName: string;
  totalFantasyPoints: number;
  players: BestBallPlayer[];
}

export interface SeasonBestBallPlayer {
  PlayerKey: string;
  PlayerName: string;
  WeeksOnRoster: number;
  WeeksStarted: number;
  TotalContributedPoints: number;
  ContributionPercent: number;
}

export interface SeasonBestBallTeam {
  TeamKey: string;
  ManagerName: string;
  WeeksPlayed: number;
  SeasonTotalBestBallPoints: number;
  BestWeekScore: number;
  WorstWeekScore: number;
  TotalRankPoints: number;
  AverageRank: number;
  Players: SeasonBestBallPlayer[];
}

export interface BestBallContext {
  Season: number;
  CurrentWeek: number;
  AvailableWeeks: number[];
}

export interface WeeklyTeamStats {
  TeamKey: string;
  ManagerName: string;
  MatchupRecord: string; // e.g., "7-3-1"
  StatValues: { [statId: string]: string }; // e.g., { "PTS": "152", "REB": "97", ... }
}

export interface WeeklyStatsSnapshot {
  Season: number;
  Week: number;
  RoundRobinResults: RoundRobinResult[];
}

export interface TeamWeeklyStats {
  TeamKey: string;
  ManagerName: string;
  StatValues: Record<string, string>; 
  // e.g. "FGM/A": "27/55", "FG%": ".491"
}

export interface RoundRobinResult {
  TeamKey: string;
  Team: TeamWeeklyStats;
  TeamRecord: TeamRoundRobinRecord;
  Matchups: RoundRobinMatchup[];
}

export interface TeamRoundRobinRecord {
  MatchupWins: number;
  MatchupLosses: number;
  MatchupTies: number;

  CategoryWins: number;
  CategoryLosses: number;
  CategoryTies: number;

  CategoryRecords: Record<string, CategoryRecord>;
}


export interface CategoryRecord {
  Category: string;
  Wins: number;
  Losses: number;
  Ties: number;
}

export interface RoundRobinMatchup {
  OpponentTeamKey: string;
  ManagerName: string;
  Wins: number;
  Losses: number;
  Ties: number;
  CategoryWins: number;
  OpponentCategoryWins: number;
  CategoryTies: number;
}
