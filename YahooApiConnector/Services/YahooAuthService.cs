using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

public class YahooAuthService
{
    private readonly IConfiguration _config;

    public YahooAuthService(IConfiguration config)
    {
        _config = config;
    }

    public string GetAuthorizationUrl()
    {
        var clientId = _config["YahooApi:ClientId"];
        var redirectUri = _config["YahooApi:RedirectUri"];
        var authURL = $"https://api.login.yahoo.com/oauth2/request_auth?client_id={clientId}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&response_type=code&language=en-us";
        return authURL;

    }

    public async Task<string> GetAccessTokenFromRefreshTokenAsync(string refreshToken)
        {
            var clientId = _config["YahooApi:ClientId"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_ID");
            var clientSecret = _config["YahooApi:ClientSecret"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_SECRET");

            using var client = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await client.PostAsync("https://api.login.yahoo.com/oauth2/get_token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();

            // Optional: update refresh token if Yahoo returns a new one
            if (doc.RootElement.TryGetProperty("refresh_token", out var newRefresh))
            {
                Console.WriteLine("Yahoo returned new refresh token (save this): " + newRefresh.GetString());
            }

            return accessToken!;
        }

    public async Task<(string accessToken, string refreshToken)> GetAccessTokenAsync(string code)
    {
        var clientId = _config["YahooApi:ClientId"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_ID");
        var clientSecret = _config["YahooApi:ClientSecret"] ?? Environment.GetEnvironmentVariable("YAHOO_CLIENT_SECRET");
        var redirectUri = _config["YahooApi:RedirectUri"];

        using var client = new HttpClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.login.yahoo.com/oauth2/get_token");
        tokenRequest.Content = new StringContent(
            $"grant_type=authorization_code&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&code={code}",
            Encoding.UTF8, "application/x-www-form-urlencoded"
        );

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var tokenResponse = await client.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

        var accessToken = tokenData.GetProperty("access_token").GetString()!;
        var refreshToken = tokenData.GetProperty("refresh_token").GetString()!;

        return (accessToken, refreshToken);
    }

}
