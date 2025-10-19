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

    public async Task<string> GetAccessTokenAsync(string code)
    {
        var clientId = _config["YahooApi:ClientId"];
        var clientSecret = _config["YahooApi:ClientSecret"];
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
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

        return tokenData.GetProperty("access_token").GetString();
    }
}
