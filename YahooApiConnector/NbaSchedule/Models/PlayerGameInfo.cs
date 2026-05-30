namespace NbaSchedule.Models;

public class PlayerGameInfo
{
    public string? PlayerKey { get; set; }
    public string? PlayerName { get; set; }
    public string? Position { get; set; }
    public string? NbaTeam { get; set; }
    public List<NbaGame> UpcomingGames { get; set; } = new();

    public bool PlaysOnDate => UpcomingGames.Count > 0;
    public int GameCount => UpcomingGames.Count;
}
