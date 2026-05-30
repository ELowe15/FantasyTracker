using System.Net.Http.Json;
using NbaSchedule.Models;

namespace NbaSchedule.Services;

public class NbaStatsService
{
    private readonly HttpClient _client;

    public NbaStatsService()
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<List<NbaGame>> FetchSeasonGamesAsync(int season)
    {
        var games = new List<NbaGame>();

        try
        {
            var startDate = new DateTime(season, 10, 1);
            var endDate = new DateTime(season + 1, 7, 1);
            var current = startDate;
            
            while (current < endDate)
            {
                var dateStr = current.ToString("yyyyMMdd");
                var url = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/scoreboard?dates={dateStr}";
                
                try
                {
                    var response = await _client.GetStringAsync(url);
                    var doc = System.Text.Json.JsonDocument.Parse(response);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("events", out var eventsArray))
                    {
                        foreach (var eventElem in eventsArray.EnumerateArray())
                        {
                            if (!eventElem.TryGetProperty("date", out var dateElem))
                                continue;
                                
                            var eventDate = dateElem.GetString() ?? "";
                            
                            if (!eventElem.TryGetProperty("competitions", out var competitionsArray))
                                continue;
                            
                            foreach (var competitionElem in competitionsArray.EnumerateArray())
                            {
                                if (!competitionElem.TryGetProperty("competitors", out var competitorArray))
                                    continue;
                                
                                if (competitorArray.GetArrayLength() < 2)
                                    continue;
                                
                                var homeTeam = competitorArray[0].GetProperty("team").GetProperty("abbreviation").GetString() ?? "UNK";
                                var awayTeam = competitorArray[1].GetProperty("team").GetProperty("abbreviation").GetString() ?? "UNK";
                                var gameId = competitionElem.GetProperty("id").GetString() ?? "";
                                
                                if (!DateTime.TryParse(eventDate, out var gameTime))
                                {
                                    gameTime = DateTime.UtcNow;
                                }
                                
                                var game = new NbaGame
                                {
                                    GameId = gameId,
                                    HomeTeam = homeTeam,
                                    AwayTeam = awayTeam,
                                    GameTime = gameTime,
                                    Season = season
                                };
                                
                                if (!games.Any(g => g.GameId == gameId))
                                {
                                    games.Add(game);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Continue on error, some dates may have no games
                }
                
                current = current.AddDays(1);
            }
            
            Console.WriteLine($"[NbaStatsService] Fetched {games.Count} games for season {season}");
            return games.OrderBy(g => g.GameTime).ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching NBA schedule: {ex.Message}");
            return new List<NbaGame>();
        }
    }
}
