namespace NbaSchedule.Models;

public class NbaWeek
{
    public int WeekNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Season { get; set; }
}
