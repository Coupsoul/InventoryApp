using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class InventoryService
    {
        private readonly Random _rnd = new Random();
        private readonly ApplicationContext _context;

        public InventoryService(ApplicationContext context)
        {
            _context = context;
        }

        public async Task<Player?> GetPlayerWithInventoryAsync(string name)
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

                    if (player == null) return $"Игрок \"{playerName}\" не найден.";
                    if (item == null) return $"Предмет \"{itemName}\" не найден.";

                    if (item.PriceCurrency == Currency.Gold)
                    {
                        if (player.Gold < item.Price)
                            return "С такими крохами кассу не открывают. Выбирай что-нибудь подешевле.";
                        player.Gold -= item.Price;
                    }
                    else
                    {
                        if (player.Gems < item.Price)
                            return "Фантазия богатая, а кошелек — не очень. Выбери что-нибудь, что будет тебе по карману.";
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
                    var player = await GetPlayerWithInventoryAsync(playerName);
                    if (player == null) return "Игрок не найден.";
                    var inventoryItem = player.Inventory.FirstOrDefault(ii => ii.Item.Name == itemName);
                    if (inventoryItem == null) return "В инвентаре нет такого предмета.";

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
                    return $"Продано: {itemName} за {sellPrice} {currencyName}.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return $"Ошибка при продаже: {ex.Message}";
                }
            }
        }


        public async Task AddRewardAsync(string playerName, int goldAmount, int gemsAmount)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
            if (player == null) return;

            player.Gold += goldAmount;
            player.Gems += gemsAmount;

            await _context.SaveChangesAsync();
        }


        public async Task<(int gold, int gems)> ProcessGrindAsync(string playerName)
        {
            int gold = _rnd.Next(10, 17);
            int gems = _rnd.Next(0, 3);

            await AddRewardAsync(playerName, gold, gems);

            return (gold, gems);
        }


        public async Task<string> ExchangeGemsAsync(string playerName, int gemsChange, int goldRate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var player = await GetPlayerWithInventoryAsync(playerName);
            if (player == null) return "Игрок не найден.";

            int goldDiff = gemsChange * goldRate;

            if (gemsChange > 0 && player.Gold < goldDiff) return "Цифры не сходятся. Для такой суммы нужно больше веса в кошельке.";
            if (gemsChange < 0 && player.Gems < Math.Abs(gemsChange)) return "Цифры не сходятся. Для такой суммы нужно больше веса в кошельке.";

            player.Gems += gemsChange;
            player.Gold -= goldDiff;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Обмен прошёл успешно.";
        }
    }
}