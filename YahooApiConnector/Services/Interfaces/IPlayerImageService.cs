using System.Collections.Generic;

namespace YahooApiConnector.Services.Interfaces
{
    public interface IPlayerImageService
    {
        Task LoadAsync();
        Task SaveAsync();
        bool TryAdd(string hashedKey, string? imageUrl);
        Dictionary<string, string> GetAll();
    }
}
