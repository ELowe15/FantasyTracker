namespace NbaSchedule.Models;

public class NbaTeamSchedule
{
    public string? TeamAbbreviation { get; set; }
    public string? TeamName { get; set; }
    public int Season { get; set; }
    public List<NbaGame> Games { get; set; } = new();
}
