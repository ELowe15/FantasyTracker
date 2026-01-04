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