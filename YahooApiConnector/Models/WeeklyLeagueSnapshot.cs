public class WeeklyLeagueSnapshot
{
    public int Season { get; set; }
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }

    public List<WeeklyTeamResult> Teams { get; set; } = new();
}
