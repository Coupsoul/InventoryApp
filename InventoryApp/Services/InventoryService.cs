using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class InventoryService
    {
        private readonly ApplicationContext _context;

        public InventoryService(ApplicationContext context)
        {
            _context = context;
        }

        public async Task<Player?> GetPlayerAsync(string name)
        {
            return await _context.Players
                .Include(p => p.Inventory)
                .ThenInclude(ii => ii.Item)
                .FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<List<Item>> GetShopItemsAsync()
        {
            return await _context.Items.ToListAsync();
        }

        public async Task<string> BuyItemAsync(string playerName, string itemName)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Name == itemName);

                    if (player == null) return $"Игрок \"{playerName}\" не найден";
                    if (item == null) return $"Предмет \"{itemName}\" не найден";

                    if (item.PriceCurrency == Currency.Gold)
                    {
                        if (player.Gold < item.Price)
                            return "Золотишка не хватило.";
                        player.Gold -= item.Price;
                    }
                    else
                    {
                        if (player.Gems < item.Price)
                            return "Маловато у тебя брюлликов.";
                        player.Gems -= item.Price;
                    }

                    var inventoryEntry = await _context.InventoryItems
                        .FirstOrDefaultAsync(ii => ii.PlayerId == player.Id && ii.ItemId == item.Id);

                    if (inventoryEntry != null)
                        inventoryEntry.Amount++;
                    else
                    {
                        var newEntry = new InventoryItem
                        {
                            PlayerId = player.Id,
                            ItemId = item.Id,
                            Amount = 1
                        };
                        _context.InventoryItems.Add(newEntry);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return $"Успешно куплено {item.Name} за {item.Price} {(item.PriceCurrency == Currency.Gold ? "золота" : "брюлликов")}.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return $"Ошибка: {ex.Message}";
                }
            }
        }

        public async Task<string> SellItemAsync(string playerName, string itemName)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var player = await GetPlayerAsync(playerName);
                    if (player == null) return "Игрок не найден";
                    var inventoryItem = player.Inventory.FirstOrDefault(ii => ii.Item.Name == itemName);
                    if (inventoryItem == null) return "В инвентаре нет такого предмета";

                    var itemData = inventoryItem.Item;
                    int sellPrice = itemData.GetSellPrice();

                    if (itemData.PriceCurrency == Currency.Gold)
                    {
                        player.Gold += sellPrice;
                    }
                    else
                    {
                        player.Gems += sellPrice;
                    }

                    inventoryItem.Amount--;

                    if (inventoryItem.Amount < 1)
                    {
                        _context.InventoryItems.Remove(inventoryItem);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var currencyName = itemData.PriceCurrency == Currency.Gold ? "золота" : "брюлликов";
                    return $"Продано: {itemName} за {sellPrice} {currencyName}";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return $"Ошибка при продаже: {ex.Message}";
                }
            }
        }

        public async Task<string> AddRewardAsync(string playerName, int goldAmount, int gemsAmount)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
            if (player == null) return "Игрок не найден.";

            player.Gold += goldAmount;
            player.Gems += gemsAmount;

            await _context.SaveChangesAsync();
            return $"Залутано {goldAmount} золота и {gemsAmount} брюлликов.";
        }
    }
}
