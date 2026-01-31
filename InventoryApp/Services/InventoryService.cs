using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Enums;
using InventoryApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly Random _rnd = new Random();
        private readonly int _minGrindGold = 10, _maxGrindGold = 17;
        private readonly int _minGrindGems = 0, _maxGrindGems = 3;
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
                        if (player.Gold + sellPrice > Player.MaxGold)
                            return $"Это нельзя продать - переполнится кошелёк.\nПределы:\n  для золота - {Player.MaxGold};\n  для брюлликов - {Player.MaxGems}.";
                        player.Gold += sellPrice;
                    }
                    else
                    {
                        if (player.Gems + sellPrice > Player.MaxGems)
                            return $"Это нельзя продать - переполнится кошелёк.\nПределы:\n  для золота - {Player.MaxGold};\n  для брюлликов - {Player.MaxGems}.";
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


        public async Task<(int gold, int gems)> ProcessGrindAsync(string playerName)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
            if (player == null) return (0, 0);

            int oldGold = player.Gold;
            int oldGems = player.Gems;

            int grindGold = _rnd.Next(_minGrindGold, _maxGrindGold);
            int grindGems = _rnd.Next(_minGrindGems, _maxGrindGems);

            player.Gold += grindGold;
            player.Gems += grindGems;

            await _context.SaveChangesAsync();

            return (player.Gold - oldGold, player.Gems - oldGems);
        }


        public async Task<string> ExchangeGemsAsync(string playerName, int gemsToExchange, int goldRate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var player = await GetPlayerWithInventoryAsync(playerName);
            if (player == null) return "Игрок не найден.";

            int goldDiff = gemsToExchange * goldRate;

            int nextGold = player.Gold - goldDiff;
            int nextGems = player.Gems + gemsToExchange;

            if (nextGold < 0 || nextGems < 0)
                return "Цифры не сходятся. Для такой суммы нужно больше веса в кошельке.";

            if (nextGold > Player.MaxGold || nextGems > Player.MaxGems)
                return $"Обмен невозможен: кошелек переполнится.\nПределы:\n  для золота - {Player.MaxGold};\n  для брюлликов - {Player.MaxGems}.";

            player.Gold = nextGold;
            player.Gems = nextGems;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Обмен прошёл успешно.";
        }


        public async Task SetBalanceAsync(string playerName, int gold, int gems)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName);
            if (player == null) return;

            player.Gold = gold;
            player.Gems = gems;

            await _context.SaveChangesAsync();
        }


        public async Task<string> CreateItemAsync(string adminName, string itemName, Currency priceCurrency, int price, string? description = null)
        {
            var admin = await _context.Players.FirstOrDefaultAsync(p => p.Name == adminName);
            if (admin == null || !admin.IsAdmin) return "Ошибка доступа: У вас нет прав учредителя.";

            var newItem = new Item(itemName, priceCurrency, price, description)
            {
                Name = itemName,
                PriceCurrency = priceCurrency,
                Price = price,
                Description = description
            };

            _context.Items.Add(newItem);
            await _context.SaveChangesAsync();
            return $"\"{itemName}\" успешно внесен в реестр.";
        }
    }
}