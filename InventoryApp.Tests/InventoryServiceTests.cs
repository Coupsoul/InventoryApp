using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Enums;
using InventoryApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InventoryApp.Tests;

public class InventoryServiceTests
{
    private ApplicationContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationContext(options);
    }


    public class GetPlayerWithInventory : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenNameIsEmpty()
        {
            var service = new InventoryService(null!);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetPlayerWithInventoryAsync("")
            );
        }


        [Fact]
        public async Task ShouldReturnPlayerWithInventory_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var player = new Player { Name = "Hero", PasswordHash = "..." };
            var item = new Item("Item", Currency.Gold, 5);

            context.Players.Add(player);
            context.Items.Add(item);
            context.InventoryItems.Add(new InventoryItem { PlayerId = player.Id, ItemId = item.Id, Amount = 1 });
            await context.SaveChangesAsync();

            var receivedPlayer = await service.GetPlayerWithInventoryAsync(player.Name);

            Assert.NotNull(receivedPlayer);
            Assert.Equal("Hero", receivedPlayer.Name);

            Assert.NotNull(receivedPlayer.Inventory);
            Assert.Single(receivedPlayer.Inventory);

            Assert.NotNull(receivedPlayer.Inventory[0].Item);
            Assert.Equal("Item", receivedPlayer.Inventory[0].Item.Name);
        }
    }


    public class GetShopItems : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldReturnAllItems()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Items.Add(new Item("Sword", Currency.Gold, 100));
            context.Items.Add(new Item("Shield", Currency.Gems, 50));
            await context.SaveChangesAsync();

            var result = await service.GetShopItemsAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, i => i.Name == "Sword");
            Assert.Contains(result, i => i.Name == "Shield");
        }
    }


    public class BuyItem : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.BuyItemAsync("Ghost", "Item")
            );
        }


        [Fact]
        public async Task ShouldThrow_WhenItemDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Hero", PasswordHash = "...", Gold = 100 });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.BuyItemAsync("Hero", "Unknown Item")
            );
        }


        [Fact]
        public async Task ShouldThrow_WhenNotEnoughGold()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Broke Gold Hero", PasswordHash = "...", Gold = 10 });
            context.Items.Add(new Item("Expensive Gold Item", Currency.Gold, 100));
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.BuyItemAsync("Broke Gold Hero", "Expensive Gold Item")
            );

            Assert.Contains("С такими крохами", ex.Message);
        }


        [Fact]
        public async Task ShouldThrow_WhenNotEnoughGems()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Broke Gem Hero", PasswordHash = "...", Gems = 2 });
            context.Items.Add(new Item("Expensive Gem Item", Currency.Gems, 20));
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.BuyItemAsync("Broke Gem Hero", "Expensive Gem Item")
            );

            Assert.Contains("Фантазия богатая,", ex.Message);
        }


        [Fact]
        public async Task ShouldReduceGoldAndAddInventoryItem_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Rich Gold Hero", PasswordHash = "...", Gold = 100 });
            context.Items.Add(new Item("Gold Stick", Currency.Gold, 10));
            await context.SaveChangesAsync();

            await service.BuyItemAsync("Rich Gold Hero", "Gold Stick");

            var player = await context.Players.Include(p => p.Inventory).FirstAsync(p => p.Name == "Rich Gold Hero");

            Assert.Equal(90, player.Gold);
            Assert.Single(player.Inventory);
            Assert.Equal("Gold Stick", player.Inventory[0].Item.Name);
        }


        [Fact]
        public async Task ShouldReduceGemsAndAddInventoryItem_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Rich Gem Hero", PasswordHash = "...", Gems = 15 });
            context.Items.Add(new Item("Gem Stick", Currency.Gems, 4));
            await context.SaveChangesAsync();

            await service.BuyItemAsync("Rich Gem Hero", "Gem Stick");

            var player = await context.Players.Include(p => p.Inventory).FirstAsync(p => p.Name == "Rich Gem Hero");

            Assert.Equal(11, player.Gems);
            Assert.Single(player.Inventory);
            Assert.Equal("Gem Stick", player.Inventory[0].Item.Name);
        }
    }


    public class SellItem : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SellItemAsync("Ghost", "Item")
            );
        }


        [Fact]
        public async Task ShouldThrow_WhenItemDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Hero", PasswordHash = "...", Gold = 100 });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SellItemAsync("Hero", "Unknown Item")
            );
        }


        [Theory]
        [InlineData(Currency.Gold, Player.MaxGold, 2)]
        [InlineData(Currency.Gems, Player.MaxGems, 2)]
        public async Task ShouldThrow_WhenMaxBalance(Currency currency, int balance, int price)
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var player = new Player { Name = "Too Rich Hero", PasswordHash = "...", Gold = 0, Gems = 0 };
            if (currency == Currency.Gold) player.Gold = balance;
            else player.Gems = balance;

            var item = new Item("Item", currency, price);

            context.Players.Add(player);
            context.Items.Add(item);
            context.InventoryItems.Add(new InventoryItem { PlayerId = player.Id, ItemId = item.Id, Amount = 1 });
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SellItemAsync("Too Rich Hero", "Item")
            );

            Assert.Contains("переполнится кошелёк", ex.Message);
        }


        [Theory]
        [InlineData(Currency.Gold, 50)]
        [InlineData(Currency.Gems, 50)]
        public async Task ShouldAddMoneyAndRemoveInventoryItem_WhenSuccessful(Currency currency, int price)
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var player = new Player { Name = "Hero", PasswordHash = "...", Gold = 0, Gems = 0 };
            var item = new Item("Item", currency, price);

            context.Players.Add(player);
            context.Items.Add(item);
            context.InventoryItems.Add(new InventoryItem { PlayerId = player.Id, ItemId = item.Id, Amount = 1 });
            await context.SaveChangesAsync();

            await service.SellItemAsync(player.Name, item.Name);

            var dbPlayer = await context.Players.Include(p => p.Inventory).FirstAsync(p => p.Name == player.Name);
            int expectedProfit = price / 2;
            int actualBalance = currency == Currency.Gold ? dbPlayer.Gold : dbPlayer.Gems;

            Assert.Equal(expectedProfit, actualBalance);
            Assert.Empty(dbPlayer.Inventory);
        }
    }


    public class ProcessGrind : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ProcessGrindAsync("Ghost")
            );
        }


        [Fact]
        public async Task ShouldIncreaseBalance_WithinLimits()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var player = new Player { Name = "Grinder", PasswordHash = "...", Gold = 10, Gems = 10 };
            context.Players.Add(player);
            await context.SaveChangesAsync();

            int oldGold = player.Gold, oldGems = player.Gems;

            var (gainedGold, gainedGems) = await service.ProcessGrindAsync(player.Name);
            var grinder = await context.Players.FirstAsync(p => p.Name == player.Name);

            Assert.Equal(grinder.Gold - oldGold, gainedGold);
            Assert.Equal(grinder.Gems - oldGems, gainedGems);

            Assert.InRange(gainedGold, 10, 17);
            Assert.InRange(gainedGems, 0, 3);
        }


        [Fact]
        public async Task ShouldClampBalance_WhenNearMax()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player
            {
                Name = "Rich Grinder",
                PasswordHash = "...",
                Gold = Player.MaxGold - 5,
                Gems = Player.MaxGems
            });
            await context.SaveChangesAsync();

            var (gainedGold, gainedGems) = await service.ProcessGrindAsync("Rich Grinder");
            var player = await context.Players.FirstAsync(p => p.Name == "Rich Grinder");

            Assert.Equal(Player.MaxGold, player.Gold);
            Assert.Equal(Player.MaxGems, player.Gems);

            Assert.Equal(5, gainedGold);
        }
    }


    public class ExchangeGems : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExchangeGemsAsync("Ghost", 1, 50)
            );
        }


        [Theory]
        [InlineData(1, 90)]
        [InlineData(-1, 90)]
        public async Task ShouldThrow_WhenNotEnoughMoney(int gemsToExchange, int goldRate)
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var player = new Player { Name = "Broke Hero", PasswordHash = "...", Gold = 2, Gems = 0 };
            context.Players.Add(player);
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExchangeGemsAsync(player.Name, gemsToExchange, goldRate)
            );
            Assert.Contains("Цифры не сходятся", ex.Message);
        }


        [Theory]
        [InlineData(1, 90)]
        [InlineData(-1, 90)]
        public async Task ShouldThrow_WhenMaxBalance(int gemsToExchange, int goldRate)
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var player = new Player { Name = "Rich Hero", PasswordHash = "...", Gold = Player.MaxGold, Gems = Player.MaxGems };
            context.Players.Add(player);
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExchangeGemsAsync(player.Name, gemsToExchange, goldRate)
            );
            Assert.Contains("кошелек переполнится", ex.Message);
        }


        [Theory]
        [InlineData(1, 90)]
        [InlineData(-1, 90)]
        public async Task ShouldExchangeCorrectAmount_WhenSuccessful(int gemsToExchange, int goldRate)
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var player = new Player { Name = "Rich Hero", PasswordHash = "...", Gold = 500, Gems = 20 };
            context.Players.Add(player);
            await context.SaveChangesAsync();

            int goldBefore = player.Gold, gemsBefore = player.Gems;
            int goldDiff = gemsToExchange * goldRate;

            await service.ExchangeGemsAsync(player.Name, gemsToExchange, goldRate);

            var actualPlayer = await context.Players.FirstAsync(p => p.Name == player.Name);
            int actualGold = actualPlayer.Gold, actualGems = actualPlayer.Gems;

            Assert.Equal(goldBefore - goldDiff, actualGold);
            Assert.Equal(gemsBefore + gemsToExchange, actualGems);
        }
    }


    public class SetBalance : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SetBalanceAsync("Ghost", 50, 50)
            );
        }


        [Fact]
        public async Task ShouldThrow_WhenCallerIsNotAdmin()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Fake-Admin", PasswordHash = "...", Gold = 0, Gems = 0 });
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.SetBalanceAsync("Fake-Admin", 50, 50)
            );

            Assert.Contains("не обладаете необходимыми привилегиями", ex.Message);
        }


        [Fact]
        public async Task ShouldUpdateBalance_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Admin", PasswordHash = "...", Gold = 0, Gems = 0, IsAdmin = true });
            await context.SaveChangesAsync();

            await service.SetBalanceAsync("Admin", 500, 150);

            var admin = await context.Players.FirstAsync(p => p.Name == "Admin");

            Assert.Equal(500, admin.Gold);
            Assert.Equal(150, admin.Gems);
        }
    }


    public class CreateItem : InventoryServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenNegativePrice()
        {
            var service = new InventoryService(null!);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateItemAsync("Ghost", "New Item", Currency.Gold, -4)
            );

            Assert.Contains("Цена не может быть отрицательной.", ex.Message);
        }


        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateItemAsync("Ghost", "New Item", Currency.Gold, 4)
            );

            Assert.Contains("Администратор не найден", ex.Message);
        }


        [Fact]
        public async Task ShouldThrow_WhenCallerIsNotAdmin()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Fake-Admin", PasswordHash = "..." });
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateItemAsync("Fake-Admin", "New Item", Currency.Gold, 4)
            );

            Assert.Contains("нет привилегий учредителя", ex.Message);
        }


        [Fact]
        public async Task ShouldThrow_WhenItemAlreadyExists()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Admin", PasswordHash = "...", IsAdmin = true });
            context.Items.Add(new Item("Stick", Currency.Gold, 4));
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateItemAsync("Admin", "Stick", Currency.Gems, 15)
            );

            Assert.Contains("уже существует", ex.Message);
        }


        [Fact]
        public async Task ShouldCreateItem_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new InventoryService(context);

            context.Players.Add(new Player { Name = "Admin", PasswordHash = "...", IsAdmin = true });
            await context.SaveChangesAsync();

            await service.CreateItemAsync("Admin", "Green Onion", Currency.Gems, 15);

            var item = await context.Items.FirstAsync(i => i.Name == "Green Onion");

            Assert.NotNull(item);
            Assert.Equal(Currency.Gems, item.PriceCurrency);
            Assert.Equal(15, item.Price);
            Assert.Null(item.Description);
        }
    }
}
