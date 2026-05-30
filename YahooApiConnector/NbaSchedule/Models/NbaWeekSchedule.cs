namespace NbaSchedule.Models;

public class NbaWeekSchedule
{
    public NbaWeek Week { get; set; } = new();
    public List<NbaGame> Games { get; set; } = new();
    public int GameCount => Games.Count;
    public List<string> TeamsPlaying
    {
        get
        {
            var teams = new HashSet<string>();
            foreach (var game in Games)
            {
                if (!string.IsNullOrEmpty(game.HomeTeam))
                    teams.Add(game.HomeTeam);
                if (!string.IsNullOrEmpty(game.AwayTeam))
                    teams.Add(game.AwayTeam);
            }
            return teams.OrderBy(t => t).ToList();
        }
    }
}
