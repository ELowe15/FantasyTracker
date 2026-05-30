namespace NbaSchedule.Models;

public class NbaGame
{
    public string? GameId { get; set; }
    public string? HomeTeam { get; set; }
    public string? AwayTeam { get; set; }
    public DateTime GameTime { get; set; }
    public int Season { get; set; }
}
