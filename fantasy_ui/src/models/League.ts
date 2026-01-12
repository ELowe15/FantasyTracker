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
  team_key: string;
  manager_name: string;
  player_key: string;
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

export type WeeklyTeamStats = {
  TeamKey: string;
  ManagerName: string;
  MatchupRecord: string; // e.g., "7-3-1"
  StatValues: { [statId: string]: string }; // e.g., { "PTS": "152", "REB": "97", ... }
};

export type WeeklyStatsSnapshot = {
  Season: number;
  Week: number;
  Teams: WeeklyTeamStats[];
};

export interface TeamRecord {
  TeamKey: string;
  MatchupWins: number;
  MatchupLosses: number;
  MatchupTies?: number;
}

export interface RawRoundRobinMatchup {
  TeamAKey: string;
  TeamBKey: string;
  TeamACategoryWins: number;
  TeamBCategoryWins: number;
}

export interface RoundRobinMatchup {
  opponentId: string;
  opponentName: string;
  teamScore: number;
  opponentScore: number;
}

export interface RoundRobinTeam {
  teamId: string;
  managerName: string;
  wins: number;
  losses: number;
  ties: number;
  rank: number;
  matchups: RoundRobinMatchup[];
}


export interface Props {
  data: {
    WeeklyTeamStats: WeeklyTeamStats[];
    RoundRobinResults: {
      TeamRecords: TeamRecord[];
      Matchups: RawRoundRobinMatchup[];
    };
  };
}

