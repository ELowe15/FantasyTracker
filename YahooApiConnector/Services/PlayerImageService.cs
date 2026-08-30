using YahooApiConnector.Services.Interfaces;

namespace YahooApiConnector.Services;

public class PlayerImageService : IPlayerImageService
{
    private readonly string _path;
    private Dictionary<string, string> _playerImages = new();

    public PlayerImageService(string path)
    {
        _path = path;
    }

    public async Task LoadAsync()
    {
        if (_playerImages.Count > 0)
            return;

        _playerImages = await YahooFantasyPersistence.LoadPlayerImagesAsync(_path);
    }

    public async Task SaveAsync()
    {
        if (_playerImages.Count == 0)
            return;

        await YahooFantasyPersistence.SavePlayerImagesAsync(_path, _playerImages);
    }

    public bool TryAdd(string hashedKey, string? imageUrl)
    {
        return YahooFantasyPersistence.TryAddPlayerImage(_playerImages, hashedKey, imageUrl);
    }

    public Dictionary<string, string> GetAll() => _playerImages;
}