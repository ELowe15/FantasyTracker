using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using YahooApiConnector.Services;
using YahooApiConnector.Services.Interfaces;

namespace YahooApiConnector;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYahooServices(
        this IServiceCollection services,
        string accessToken,
        string imageOutPath)
    {
        services.AddSingleton<YahooFantasyApiClient>(_ =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            return new YahooFantasyApiClient(client);
        });

        services.AddSingleton<IPlayerImageService>(_ => new PlayerImageService(imageOutPath));
        services.AddSingleton<IRosterService, RosterService>();
        services.AddSingleton<IStatsService, StatsService>();
        services.AddSingleton<ISnapshotService, SnapshotService>();
        services.AddSingleton<YahooFantasyService>();
        services.AddSingleton<YahooAccountService>();

        return services;
    }
}
