public class TeamWeeklyStats
{
    public string TeamKey { get; set; }
    public string ManagerName { get; set; }
    public Dictionary<string, string> StatValues { get; set; } = new();
}
