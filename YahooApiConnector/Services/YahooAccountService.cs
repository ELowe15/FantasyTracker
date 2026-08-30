using System.Xml.Linq;

namespace YahooApiConnector.Services;

public class YahooAccountService
{
    private readonly YahooFantasyApiClient _apiClient;

    public YahooAccountService(YahooFantasyApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Print teams for any league (any sport) given a league key
    public async Task PrintLeagueTeamsAsync(string leagueKey)
    {
        Console.WriteLine($"[YahooAccountService] PrintLeagueTeamsAsync league={leagueKey}");
        try
        {
            var teams = await _apiClient.GetLeagueTeamsAsync(leagueKey);
            Console.WriteLine($"[YahooAccountService] Found {teams.Count} teams for league {leagueKey}");
            foreach (var t in teams)
            {
                Console.WriteLine($" - TeamKey={t.TeamKey} Manager={t.ManagerName}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[YahooAccountService] Failed to print league teams: {ex.Message}");
            throw;
        }
    }

    // Fetch a raw document from any Yahoo fantasy endpoint and print root element info
    public async Task<XDocument> FetchAndPrintRawAsync(string url)
    {
        Console.WriteLine($"[YahooAccountService] FetchAndPrintRawAsync url={url}");
        var doc = await _apiClient.GetDocumentAsync(url);
        var ns = YahooFantasyApiClient.GetNamespace(doc);
        Console.WriteLine($"[YahooAccountService] Root element: {doc.Root?.Name}");
        var children = doc.Root?.Elements().Take(10).Select(e => e.Name.LocalName).ToList() ?? new List<string>();
        Console.WriteLine($"[YahooAccountService] Top-level children (up to 10): {string.Join(",", children)}");
        return doc;
    }

    // Print the raw XML for all teams associated with the logged-in Yahoo account.
    // This does not require a league key and will show teams across all games/sports.
    public async Task<XDocument> PrintUserTeamsXmlAsync()
    {
        Console.WriteLine("[YahooAccountService] PrintUserTeamsXmlAsync for logged-in user");
        // Use the account-scoped endpoint which relies on the authenticated user's session
        var url = "https://fantasysports.yahooapis.com/fantasy/v2/users;use_login=1/games/teams";
        var doc = await _apiClient.GetDocumentAsync(url);
        var ns = YahooFantasyApiClient.GetNamespace(doc);

        Console.WriteLine($"[YahooAccountService] Root element: {doc.Root?.Name}");
        // Print a short summary of top-level child elements
        var children = doc.Root?.Elements().Take(10).Select(e => e.Name.LocalName).ToList() ?? new List<string>();
        Console.WriteLine($"[YahooAccountService] Top-level children (up to 10): {string.Join(",", children)}");

        // Print the full XML (pretty-printed via ToString)
        Console.WriteLine("[YahooAccountService] Full XML output:");
        Console.WriteLine(doc.ToString());

        return doc;
    }
}
