using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;

public class YahooFantasyService
{
    private readonly HttpClient _client;

    public YahooFantasyService(string accessToken)
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<LeagueData> GetLeagueDataAsync(string leagueKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}";
        var response = await _client.GetAsync(url);
        var xml = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Raw Yahoo API Response:");
        Console.WriteLine(xml);

        try
        {
            var doc = XDocument.Parse(xml);

            // Get the default namespace from the document
            XNamespace ns = doc.Root.GetDefaultNamespace();

            // Find the <league> element using the namespace
            var leagueElem = doc.Descendants(ns + "league").FirstOrDefault();
            if (leagueElem == null)
            {
                Console.WriteLine("No <league> element found in response.");
                return null;
            }

            return new LeagueData
            {
                LeagueKey = leagueElem.Element(ns + "league_key")?.Value,
                Name = leagueElem.Element(ns + "name")?.Value,
                Season = leagueElem.Element(ns + "season")?.Value
                // Add more properties as needed
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing XML:");
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task GetLeagueTeamsAsync(string leagueKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
        var response = await _client.GetAsync(url);
        var xml = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Raw Teams XML:");
        Console.WriteLine(xml);

        var doc = XDocument.Parse(xml);
        XNamespace ns = doc.Root.GetDefaultNamespace();

        var teams = doc.Descendants(ns + "team")
            .Select(t => new {
                TeamKey = t.Element(ns + "team_key")?.Value,
                Name = t.Element(ns + "name")?.Value
            });

        foreach (var team in teams)
        {
            Console.WriteLine($"Team: {team.Name} (Key: {team.TeamKey})");
        }
    }

    public async Task GetTeamRosterAsync(string teamKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/team/{teamKey}/roster";
        var response = await _client.GetAsync(url);
        var xml = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Raw Roster XML:");
        Console.WriteLine(xml);

        var doc = XDocument.Parse(xml);
        XNamespace ns = doc.Root.GetDefaultNamespace();

        var players = doc.Descendants(ns + "player")
            .Select(p => new {
                PlayerKey = p.Element(ns + "player_key")?.Value,
                FullName = p.Element(ns + "name")?.Element(ns + "full")?.Value,
                Position = p.Element(ns + "display_position")?.Value
            });

        foreach (var player in players)
        {
            Console.WriteLine($"{player.FullName} - {player.Position}");
        }
    }

    public async Task GetDraftResultsAsync(string leagueKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/draftresults";
        var response = await _client.GetAsync(url);
        var xml = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Raw Draft Results XML:");
        Console.WriteLine(xml);

        var doc = XDocument.Parse(xml);
        XNamespace ns = doc.Root.GetDefaultNamespace();

        var results = doc.Descendants(ns + "draft_result")
            .Select(dr => new
            {
                Pick = dr.Element(ns + "pick")?.Value,
                Round = dr.Element(ns + "round")?.Value,
                TeamKey = dr.Element(ns + "team_key")?.Value,
                PlayerKey = dr.Element(ns + "player_key")?.Value
            });

        foreach (var result in results)
        {
            Console.WriteLine($"Pick {result.Pick} (Round {result.Round}) → Team {result.TeamKey}, Player {result.PlayerKey}");
        }
    }
    
    public async Task DumpAllTeamRostersToJsonAsync(string leagueKey, string outputPath)
{
    // 1. Get all teams and build teamKey → teamName/manager map
    var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
    var teamsResponse = await _client.GetAsync(teamsUrl);
    var teamsXml = await teamsResponse.Content.ReadAsStringAsync();
    var teamsDoc = XDocument.Parse(teamsXml);
    XNamespace ns = teamsDoc.Root.GetDefaultNamespace();

    var teams = teamsDoc.Descendants(ns + "team")
        .Select(t => new {
            TeamKey = t.Element(ns + "team_key")?.Value,
            ManagerName = t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value
        })
        .ToList();

    var allRosters = new List<TeamRoster>();

    // 2. For each team, fetch roster
    foreach (var team in teams)
    {
        var rosterUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/team/{team.TeamKey}/roster";
        var rosterResponse = await _client.GetAsync(rosterUrl);
        var rosterXml = await rosterResponse.Content.ReadAsStringAsync();
        var rosterDoc = XDocument.Parse(rosterXml);

        var players = rosterDoc.Descendants(ns + "player")
            .Select(p => new Player
            {
                PlayerKey = p.Element(ns + "player_key")?.Value,
                FullName = p.Element(ns + "name")?.Element(ns + "full")?.Value,
                Position = p.Element(ns + "display_position")?.Value,
                NbaTeam = p.Element(ns + "editorial_team_abbr")?.Value
            })
            .ToList();

        allRosters.Add(new TeamRoster
        {
            TeamKey = team.TeamKey,
            ManagerName = team.ManagerName,
            Players = players
        });
    }

    // 3. Serialize to JSON and save
    var json = JsonSerializer.Serialize(allRosters, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(outputPath, json);
    Console.WriteLine($"All team rosters saved to {outputPath}");
}

    public async Task DumpDraftResultsToJsonAsync(string leagueKey, string outputPath)
    {
        // 1. Get all teams and build teamKey → teamName map
        var teamsUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
        var teamsResponse = await _client.GetAsync(teamsUrl);
        var teamsXml = await teamsResponse.Content.ReadAsStringAsync();
        var teamsDoc = XDocument.Parse(teamsXml);
        XNamespace ns = teamsDoc.Root.GetDefaultNamespace();

        var teamMap = teamsDoc.Descendants(ns + "team")
            .ToDictionary(
                t => t.Element(ns + "team_key")?.Value,
                t => new {
                    ManagerName = t.Descendants(ns + "manager").FirstOrDefault()?.Element(ns + "nickname")?.Value
                }
            );

        // 2. Get all draft results
        var draftUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/draftresults";
        var draftResponse = await _client.GetAsync(draftUrl);
        var draftXml = await draftResponse.Content.ReadAsStringAsync();
        var draftDoc = XDocument.Parse(draftXml);

        var draftResults = draftDoc.Descendants(ns + "draft_result")
            .Select(dr => new
            {
                round = int.Parse(dr.Element(ns + "round")?.Value ?? "0"),
                pick = int.Parse(dr.Element(ns + "pick")?.Value ?? "0"),
                team_key = dr.Element(ns + "team_key")?.Value,
                player_key = dr.Element(ns + "player_key")?.Value
            })
            .ToList();

        // 3. Optionally fetch player names and positions
        var resultsWithNames = new List<object>();
        foreach (var dr in draftResults)
        {
            string playerName = null;
            string position = null;

            // Fetch player info
            if (!string.IsNullOrEmpty(dr.player_key))
            {
                var playerUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/player/{dr.player_key}";
                var playerResponse = await _client.GetAsync(playerUrl);
                var playerXml = await playerResponse.Content.ReadAsStringAsync();
                var playerDoc = XDocument.Parse(playerXml);

                var playerElem = playerDoc.Descendants(ns + "player").FirstOrDefault();
                playerName = playerElem?.Element(ns + "name")?.Element(ns + "full")?.Value;
                position = playerElem?.Element(ns + "display_position")?.Value;
            }

            resultsWithNames.Add(new
            {
                dr.round,
                dr.pick,
                dr.team_key,
                manager_name = teamMap.ContainsKey(dr.team_key) ? teamMap[dr.team_key].ManagerName : null,
                dr.player_key,
                player_name = playerName,
                position = position
            });
        }

        // 4. Serialize to JSON and save
        var json = JsonSerializer.Serialize(resultsWithNames, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        Console.WriteLine($"Draft results saved to {outputPath}");
    }
    
    public async Task<string> GetRawXmlAsync(string leagueKey, string endpoint, string _accessToken)
    {
        var baseUrl = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/";
        var url = $"{baseUrl}{endpoint}";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync();
        return xml;
    }

    public async Task GetDraftedPlayersAsJsonAsync(string leagueKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/draftresults";
        var response = await _client.GetAsync(url);
        var xml = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Raw Draft Results XML:");
        Console.WriteLine(xml);

        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            var draftResults = doc.Descendants(ns + "draft_result")
                .Select(dr => new
                {
                    Pick = dr.Element(ns + "pick")?.Value,
                    Round = dr.Element(ns + "round")?.Value,
                    TeamKey = dr.Element(ns + "team_key")?.Value,
                    PlayerKey = dr.Element(ns + "player_key")?.Value
                })
                .ToList();

            // Convert to JSON string
            var json = JsonSerializer.Serialize(draftResults, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("Draft Results JSON:");
            Console.WriteLine(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing Draft Results XML:");
            Console.WriteLine(ex.Message);
        }
    }


    public async Task SaveLeagueDataAsJsonAsync(LeagueData data, string outputPath)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);
    }

    public async Task ListUserLeaguesAsync()
    {
        var url = "https://fantasysports.yahooapis.com/fantasy/v2/users;use_login=1/games/leagues";
        var response = await _client.GetAsync(url);
        var xml = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Raw leagues XML response:");
        Console.WriteLine(xml);

        try
        {
            var doc = XDocument.Parse(xml);
            var leagueKeys = doc.Descendants("league_key").Select(x => x.Value).ToList();

            Console.WriteLine("Your league keys:");
            foreach (var key in leagueKeys)
            {
                Console.WriteLine(key);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing leagues XML:");
            Console.WriteLine(ex.Message);
        }
    }
}