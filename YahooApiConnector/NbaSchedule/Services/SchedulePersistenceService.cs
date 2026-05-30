using System.Text.Json;
using NbaSchedule.Models;

namespace NbaSchedule.Services;

public class SchedulePersistenceService
{
    private readonly string _baseDataPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SchedulePersistenceService(string baseDataPath)
    {
        _baseDataPath = baseDataPath;
    }

    public string GetScheduleDirectory(int season)
    {
        return Path.Combine(_baseDataPath, "nba", season.ToString(), "schedule");
    }

    public void EnsureDirectoryExists(int season)
    {
        var dir = GetScheduleDirectory(season);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public async Task SaveSeasonScheduleAsync(List<NbaGame> games, int season)
    {
        try
        {
            EnsureDirectoryExists(season);
            var filePath = Path.Combine(GetScheduleDirectory(season), "season_schedule.json");
            var json = JsonSerializer.Serialize(games, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine($"[SchedulePersistence] Saved {games.Count} games to {filePath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error saving season schedule: {ex.Message}");
            throw;
        }
    }

    public async Task SaveWeeklyScheduleAsync(List<NbaWeekSchedule> weekSchedules, int season)
    {
        try
        {
            EnsureDirectoryExists(season);
            var weeksDir = Path.Combine(GetScheduleDirectory(season), "weeks");
            
            if (!Directory.Exists(weeksDir))
                Directory.CreateDirectory(weeksDir);

            foreach (var weekSchedule in weekSchedules)
            {
                var weekFilePath = Path.Combine(weeksDir, $"week_{weekSchedule.Week.WeekNumber:D2}.json");
                var json = JsonSerializer.Serialize(weekSchedule, _jsonOptions);
                await File.WriteAllTextAsync(weekFilePath, json);
            }

            Console.WriteLine($"[SchedulePersistence] Saved {weekSchedules.Count} weeks to {weeksDir}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error saving weekly schedule: {ex.Message}");
            throw;
        }
    }

    public async Task<NbaWeekSchedule> LoadWeekScheduleAsync(int season, int weekNumber)
    {
        try
        {
            var weekFilePath = Path.Combine(GetScheduleDirectory(season), "weeks", $"week_{weekNumber:D2}.json");
            
            if (!File.Exists(weekFilePath))
                return new NbaWeekSchedule();

            var json = await File.ReadAllTextAsync(weekFilePath);
            return JsonSerializer.Deserialize<NbaWeekSchedule>(json) ?? new NbaWeekSchedule();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error loading week {weekNumber} schedule: {ex.Message}");
            return new NbaWeekSchedule();
        }
    }

    public async Task<List<NbaWeekSchedule>> LoadAllWeeksAsync(int season)
    {
        try
        {
            var weeksDir = Path.Combine(GetScheduleDirectory(season), "weeks");
            
            if (!Directory.Exists(weeksDir))
                return new List<NbaWeekSchedule>();

            var weekSchedules = new List<NbaWeekSchedule>();
            var weekFiles = Directory.GetFiles(weeksDir, "week_*.json")
                .OrderBy(f => f)
                .ToList();

            foreach (var weekFile in weekFiles)
            {
                var json = await File.ReadAllTextAsync(weekFile);
                var weekSchedule = JsonSerializer.Deserialize<NbaWeekSchedule>(json);
                if (weekSchedule != null)
                    weekSchedules.Add(weekSchedule);
            }

            return weekSchedules;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error loading all weeks: {ex.Message}");
            return new List<NbaWeekSchedule>();
        }
    }

    public async Task<List<NbaGame>> LoadSeasonScheduleAsync(int season)
    {
        try
        {
            var filePath = Path.Combine(GetScheduleDirectory(season), "season_schedule.json");
            
            if (!File.Exists(filePath))
                return new List<NbaGame>();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<NbaGame>>(json) ?? new List<NbaGame>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error loading season schedule: {ex.Message}");
            return new List<NbaGame>();
        }
    }

    public async Task SaveTeamsAsync(List<NbaTeam> teams, int season)
    {
        try
        {
            EnsureDirectoryExists(season);
            var filePath = Path.Combine(GetScheduleDirectory(season), "teams.json");
            var json = JsonSerializer.Serialize(teams, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine($"[SchedulePersistence] Saved {teams.Count} teams to {filePath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error saving teams: {ex.Message}");
            throw;
        }
    }

    public async Task<List<NbaTeam>> LoadTeamsAsync(int season)
    {
        try
        {
            var filePath = Path.Combine(GetScheduleDirectory(season), "teams.json");
            
            if (!File.Exists(filePath))
                return new List<NbaTeam>();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<NbaTeam>>(json) ?? new List<NbaTeam>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error loading teams: {ex.Message}");
            return new List<NbaTeam>();
        }
    }

    public async Task SaveDayGamesAsync(List<NbaGame> games, int season, DateTime date)
    {
        try
        {
            var dayDir = Path.Combine(GetScheduleDirectory(season), "games");
            if (!Directory.Exists(dayDir))
                Directory.CreateDirectory(dayDir);

            var filePath = Path.Combine(dayDir, $"{date:yyyy-MM-dd}.json");
            var json = JsonSerializer.Serialize(games, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine($"[SchedulePersistence] Saved {games.Count} games for {date:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error saving day games: {ex.Message}");
            throw;
        }
    }

    public async Task<List<NbaGame>> LoadDayGamesAsync(int season, DateTime date)
    {
        try
        {
            var filePath = Path.Combine(GetScheduleDirectory(season), "games", $"{date:yyyy-MM-dd}.json");
            
            if (!File.Exists(filePath))
                return new List<NbaGame>();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<NbaGame>>(json) ?? new List<NbaGame>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SchedulePersistence] Error loading day games: {ex.Message}");
            return new List<NbaGame>();
        }
    }
}

