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
            FastFailArgument(name, "Имя игрока не может быть пустым.");

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
            FastFailArgument(playerName, "Имя игрока не может быть пустым.");
            FastFailArgument(itemName, "Название предмета не может быть пустым.");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName)
                        ?? throw new InvalidOperationException($"Игрок \"{playerName}\" не найден.");
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Name == itemName)
                        ?? throw new InvalidOperationException($"Предмет \"{itemName}\" не найден.");

                    if (item.PriceCurrency == Currency.Gold)
                    {
                        if (player.Gold < item.Price)
                            throw new InvalidOperationException("С такими крохами кассу не открывают. Выбирай что-нибудь подешевле.");
                        player.Gold -= item.Price;
                    }
                    else
                    {
                        if (player.Gems < item.Price)
                            throw new InvalidOperationException("Фантазия богатая, а кошелек — не очень. Выбери что-нибудь, что будет тебе по карману.");
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
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }


        public async Task<string> SellItemAsync(string playerName, string itemName)
        {
            FastFailArgument(playerName, "Имя игрока не может быть пустым.");
            FastFailArgument(itemName, "Название предмета не может быть пустым.");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var player = await GetPlayerWithInventoryAsync(playerName)
                        ?? throw new InvalidOperationException($"Игрок \"{playerName}\" не найден.");
                    var inventoryItem = player.Inventory.FirstOrDefault(ii => ii.Item.Name == itemName)
                        ?? throw new InvalidOperationException($"В инвентаре нет предмета \"{itemName}\".");

                    var itemData = inventoryItem.Item;
                    int sellPrice = itemData.GetSellPrice();

                    if (itemData.PriceCurrency == Currency.Gold)
                    {
                        if (player.Gold + sellPrice > Player.MaxGold)
                            throw new InvalidOperationException($"Это нельзя продать - переполнится кошелёк.\nПределы:\n  для золота - {Player.MaxGold};\n  для брюлликов - {Player.MaxGems}.");
                        player.Gold += sellPrice;
                    }
                    else
                    {
                        if (player.Gems + sellPrice > Player.MaxGems)
                            throw new InvalidOperationException($"Это нельзя продать - переполнится кошелёк.\nПределы:\n  для золота - {Player.MaxGold};\n  для брюлликов - {Player.MaxGems}.");
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
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }


        public async Task<(int gold, int gems)> ProcessGrindAsync(string playerName)
        {
            FastFailArgument(playerName, "Имя игрока не может быть пустым.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName)
                ?? throw new InvalidOperationException($"Игрок \"{playerName}\" не найден.");

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
            FastFailArgument(playerName, "Имя игрока не может быть пустым.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            var player = await GetPlayerWithInventoryAsync(playerName)
                ?? throw new InvalidOperationException("Игрок не найден.");

            long goldDiff = (long)gemsToExchange * goldRate;

            long nextGold = player.Gold - goldDiff;
            long nextGems = player.Gems + gemsToExchange;

            if (nextGold < 0 || nextGems < 0)
                throw new InvalidOperationException("Цифры не сходятся. Для такой суммы нужно больше веса в кошельке.");

            if (nextGold > Player.MaxGold || nextGems > Player.MaxGems)
                throw new InvalidOperationException($"Обмен невозможен: кошелек переполнится.\nПределы:\n  для золота - {Player.MaxGold};\n  для брюлликов - {Player.MaxGems}.");

            player.Gold = (int)nextGold;
            player.Gems = (int)nextGems;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Обмен прошёл успешно.";
        }


        public async Task SetBalanceAsync(string playerName, int gold, int gems)
        {
            FastFailArgument(playerName, "Имя игрока не может быть пустым.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Name == playerName)
                ?? throw new InvalidOperationException($"Игрок \"{playerName}\" не найден.");

            player.Gold = gold;
            player.Gems = gems;

            await _context.SaveChangesAsync();
        }


        public async Task<string> CreateItemAsync(string adminName, string itemName, Currency priceCurrency, int price, string? description = null)
        {
            FastFailArgument(adminName, "Имя администратора не указано.");
            FastFailArgument(itemName, "Название предмета не может быть пустым.");
            if (price < 0) throw new ArgumentException("Цена не может быть отрицательной.");

            var admin = await _context.Players.FirstOrDefaultAsync(p => p.Name == adminName)
                ?? throw new InvalidOperationException("Администратор не найден.");

            if (!admin.IsAdmin) throw new InvalidOperationException("Ошибка доступа: У вас нет прав учредителя.");

            bool itemExists = await _context.Items.AnyAsync(i => i.Name == itemName);
            if (itemExists) throw new ArgumentException($"Предмет с именем \"{itemName}\" уже существует.");

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


        private void FastFailArgument(string arg, string message)
        {
            if (string.IsNullOrWhiteSpace(arg))
                throw new ArgumentException(message, nameof(arg));
        }
    }
}