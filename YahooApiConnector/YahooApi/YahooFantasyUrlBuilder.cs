public static class YahooFantasyUrlBuilder
{
    private const string BaseUrl = "https://fantasysports.yahooapis.com/fantasy/v2";

    public static string LeagueSettings(string leagueKey) => $"{BaseUrl}/league/{leagueKey}/settings";
    public static string League(string leagueKey) => $"{BaseUrl}/league/{leagueKey}";
    public static string LeagueTeams(string leagueKey) => $"{BaseUrl}/league/{leagueKey}/teams";
    public static string LeagueTeamsWithPlayers(string leagueKey) => $"{BaseUrl}/league/{leagueKey}/teams;out=players";
    public static string LeagueScoreboard(string leagueKey, int week) => $"{BaseUrl}/league/{leagueKey}/scoreboard;week={week}";
    public static string LeagueDraftResults(string leagueKey) => $"{BaseUrl}/league/{leagueKey}/draftresults";

    public static string TeamStats(string teamKey, int week) => $"{BaseUrl}/team/{teamKey}/stats;type=week;week={week}";
    public static string TeamSeasonStats(string teamKey) => $"{BaseUrl}/team/{teamKey}/stats";
    public static string TeamRoster(string teamKey, DateTime? date = null)
    {
        if (date.HasValue)
            return $"{BaseUrl}/team/{teamKey}/roster;date={date.Value:yyyy-MM-dd}";

        return $"{BaseUrl}/team/{teamKey}/roster";
    }

    public static string PlayersStatsByDate(IEnumerable<string> playerKeys, DateTime date)
    {
        var keysCsv = string.Join(",", playerKeys);
        return $"{BaseUrl}/players;player_keys={keysCsv}/stats;type=date;date={date:yyyy-MM-dd}";
    }

    public static string PlayerStatsByDate(string playerKey, DateTime date) =>
        $"{BaseUrl}/player/{playerKey}/stats;type=date;date={date:yyyy-MM-dd}";

    public static string PlayerDetails(string playerKey) => $"{BaseUrl}/player/{playerKey}";
}
