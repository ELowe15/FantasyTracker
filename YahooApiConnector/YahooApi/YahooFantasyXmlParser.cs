using System.Xml.Linq;

public static class YahooFantasyXmlParser
{
    public static XNamespace GetNamespace(XDocument doc) => doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

    public static int GetSeason(XDocument doc, int fallbackYear)
    {
        var ns = GetNamespace(doc);
        var seasonStr = doc.Descendants(ns + "season").FirstOrDefault()?.Value;
        return int.TryParse(seasonStr, out var season) ? season : fallbackYear;
    }

    public static int GetCurrentWeek(XDocument doc, int fallbackWeek = 1)
    {
        var ns = GetNamespace(doc);
        var weekStr = doc.Descendants(ns + "current_week").FirstOrDefault()?.Value;
        return int.TryParse(weekStr, out var week) ? week : fallbackWeek;
    }

    public static (DateTime start, DateTime end) GetWeekDateRange(XDocument doc)
    {
        var ns = GetNamespace(doc);
        var startStr = doc.Descendants(ns + "week_start").FirstOrDefault()?.Value;
        var endStr = doc.Descendants(ns + "week_end").FirstOrDefault()?.Value;

        DateTime.TryParse(startStr, out var start);
        DateTime.TryParse(endStr, out var end);

        return (start, end);
    }

    public static List<YahooTeamSummary> ParseTeams(XDocument doc)
    {
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

    public static Dictionary<string, double> ParseStatDictionary(XElement playerNode, XNamespace ns)
    {
        var statDict = new Dictionary<string, double>();

        foreach (var s in playerNode.Descendants(ns + "stat"))
        {
            var statId = s.Element(ns + "stat_id")?.Value;
            var statKey = Helpers.GetStatDisplayName(statId ?? string.Empty);

            if (!string.IsNullOrEmpty(statKey) &&
                double.TryParse(s.Element(ns + "value")?.Value, out var val))
            {
                statDict[statKey] = val;
            }
        }

        return Helpers.FilterToBestBallStats(statDict);
    }

    public static WeeklyPlayerStats? ParsePlayerNodeToWeeklyPlayerStats(XElement playerNode, XNamespace ns)
    {
        var playerKey = playerNode.Element(ns + "player_key")?.Value;
        if (string.IsNullOrWhiteSpace(playerKey))
            return null;

        var rawStats = ParseStatDictionary(playerNode, ns);
        if (rawStats.Count == 0)
            return null;

        return new WeeklyPlayerStats
        {
            PlayerKey = Helpers.Hash(playerKey),
            FullName = playerNode.Element(ns + "name")?.Element(ns + "full")?.Value,
            Position = playerNode.Element(ns + "display_position")?.Value,
            NbaTeam = playerNode.Element(ns + "editorial_team_abbr")?.Value,
            RawStats = rawStats,
            FantasyPoints = Helpers.ComputeFantasyPoints(rawStats)
        };
    }
}
