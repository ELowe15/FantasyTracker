using System.Xml.Linq;

public sealed class YahooFantasyApiClient
{
    private readonly HttpClient _client;

    public YahooFantasyApiClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<XDocument> GetDocumentAsync(string url)
    {
    using var req = new HttpRequestMessage(HttpMethod.Get, url);

    var resp = await _client.SendAsync(req);

    var content = await resp.Content.ReadAsStringAsync();

    if (!resp.IsSuccessStatusCode)
    {
        Console.Error.WriteLine(
            $"[YahooFantasyApiClient] Non-success response body: " +
            $"{content.Substring(0, Math.Min(1000, content.Length))}");

        resp.EnsureSuccessStatusCode();
    }

    return XDocument.Parse(content);
    }

    public static XNamespace GetNamespace(XDocument doc)
    {
        return doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
    }

    public async Task<List<YahooTeamSummary>> GetLeagueTeamsAsync(string leagueKey)
    {
        var url = $"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueKey}/teams";
        var doc = await GetDocumentAsync(url);
        var ns = GetNamespace(doc);

        return doc.Descendants(ns + "team")
            .Select(t => new YahooTeamSummary(
                t.Element(ns + "team_key")?.Value ?? string.Empty,
                t.Descendants(ns + "manager")
                    .FirstOrDefault()?
                    .Element(ns + "nickname")?
                    .Value ?? "Unknown"))
            .ToList();
    }
}

public sealed record YahooTeamSummary(string TeamKey, string ManagerName);
