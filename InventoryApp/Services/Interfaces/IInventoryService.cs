using InventoryApp.Entities;
using InventoryApp.Enums;

namespace InventoryApp.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<Player?> GetPlayerWithInventoryAsync(string name);

        Task<List<Item>> GetShopItemsAsync();

        Task<string> BuyItemAsync(string playerName, string itemName);

        Task<string> SellItemAsync(string playerName, string itemName);

        Task<(int gold, int gems)> ProcessGrindAsync(string playerName);

        Task<string> ExchangeGemsAsync(string playerName, int gemsChange, int goldRate);

        Task SetBalanceAsync(string playerName, int gold, int gems);

        Task<string> CreateItemAsync(string adminName, string itemName, Currency priceCurrency, int price, string? description = null);
    }
}