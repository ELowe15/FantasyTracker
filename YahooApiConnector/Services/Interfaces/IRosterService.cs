using System;
using System.Collections.Generic;

namespace YahooApiConnector.Services.Interfaces
{
    public interface IRosterService
    {
        Task<List<TeamRoster>> GetTeamRostersForDateAsync(string leagueKey, DateTime date);
        Task DumpAllTeamRostersToJsonAsync(string leagueKey, string outputPath);
    }
}
