public class LeagueContext
{
    public int Season { get; set; }
    public int CurrentWeek { get; set; }
    public List<int> AvailableWeeks { get; set; } = new();
}
