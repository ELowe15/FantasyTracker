using System.Security.Cryptography;
using System.Text;

public static class Helpers
{
    public static string GetDisplayManagerName(string? managerName)
    {
        if (string.IsNullOrWhiteSpace(managerName))
            return managerName ?? string.Empty;

        return managerName switch
        {
            "evan" => "EFry",
            "Evan" => "ELowe",
            _ => managerName
        };
    }

    public static string Hash(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash); // .NET 5+
    }

    public static double ComputeFantasyPoints(Dictionary<string, double> stats)
    {
        double points = 0;

        foreach (var kv in stats)
        {
            switch (kv.Key)
            {
                case "PTS": points += kv.Value * 1;   break; // Points
                case "REB": points += kv.Value * 1.2; break; // Rebounds
                case "AST": points += kv.Value * 1.5; break; // Assists
                case "STL": points += kv.Value * 3;   break; // Steals
                case "BLK": points += kv.Value * 3;   break; // Blocks
                case "TO": points -= kv.Value * 1;   break; // Turnovers
            }
        }

        return points;
    }

    // List of standard Best Ball stat keys you want to keep
    private static readonly HashSet<string> BestBallStats = new()
    {
        "PTS", "REB", "AST", "STL", "BLK", "TO"
    };

    public static Dictionary<string, double> FilterToBestBallStats(Dictionary<string, double> rawStats)
    {
        return rawStats
            .Where(kv => BestBallStats.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public static string GetStatDisplayName(string statId)
    {
        if (string.IsNullOrWhiteSpace(statId))
            return string.Empty;

        return statId switch
        {
            "12" => "PTS",
            "15" => "REB",
            "16" => "AST",
            "17" => "STL",
            "18" => "BLK",
            "19" => "TO",
            "10" => "3PM",
            "5"  => "FG%",
            "8"  => "FT%",
            "9004003" => "FGM/A",
            "9007006" => "FTM/A",
            _    => statId
        };
    }
}
