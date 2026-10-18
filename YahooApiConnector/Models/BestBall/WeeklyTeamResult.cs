public class WeeklyTeamResult
{
    public int Season { get; set; }
    public int Week { get; set; }

    public string TeamKey { get; set; } = "";
    public string ManagerName { get; set; } = "";

    public List<WeeklyPlayerStats> Players { get; set; } = new();

    public double TotalBestBallPoints { get; set; } = 0;
}