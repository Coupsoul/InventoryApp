using InventoryApp.Entities;

namespace InventoryApp.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<Player?> GetPlayerWithInventoryAsync(string name);

        Task<List<Item>> GetShopItemsAsync();

        Task<string> BuyItemAsync(string playerName, string itemName);

        Task<string> SellItemAsync(string playerName, string itemName);

        Task AddRewardAsync(string playerName, int goldAmount, int gemsAmount);

        Task<(int gold, int gems)> ProcessGrindAsync(string playerName);

        Task<string> ExchangeGemsAsync(string playerName, int gemsChange, int goldRate);
    }
}