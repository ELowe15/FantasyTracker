public class WeeklyStatsSnapshot
{
    public int Season { get; set; }
    public int Week { get; set; }

    public List<RoundRobinResult> RoundRobinResults { get; set; } = new();
}

