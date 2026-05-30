using Microsoft.Extensions.Configuration;

public class CommandRouter
{
    private readonly IConfiguration _configuration;
    private readonly string _dataPath;

    public CommandRouter(IConfiguration configuration, string dataPath)
    {
        _configuration = configuration;
        _dataPath = dataPath;
    }

    public async Task<int> RouteAsync(string[] args)
    {
        if (args.Length == 0)
            return ExecuteDefaultPipeline();

        return args[0] switch
        {
            "--fetch-nba-schedule" => await ExecuteNbaScheduleAsync(args),
            _ => ExecuteDefaultPipeline()
        };
    }

    private async Task<int> ExecuteNbaScheduleAsync(string[] args)
    {
        var command = new FetchNbaScheduleCommand(_configuration, _dataPath);
        return await command.ExecuteAsync(args);
    }

    private int ExecuteDefaultPipeline()
    {
        return 0;
    }
}
