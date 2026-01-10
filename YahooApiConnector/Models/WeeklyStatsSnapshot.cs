public class WeeklyStatsSnapshot
{
    public int Season { get; set; }
    public int Week { get; set; }

    public List<TeamWeeklyStats> Teams { get; set; } = new();

    public RoundRobinResult RoundRobinResults { get; set; } = new();
}
