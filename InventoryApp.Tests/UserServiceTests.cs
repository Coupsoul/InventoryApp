using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InventoryApp.Tests;

public class UserServiceTests
{
    private ApplicationContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationContext(options);
    }


    public class GetPlayer : UserServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenNameIsEmpty()
        {
            var service = new UserService(null!);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetPlayerAsync("")
            );
        }


        [Fact]
        public async Task ShouldReturnPlayer_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            context.Players.Add(new Player { Name = "User", PasswordHash = "...", Gold = 15 });
            await context.SaveChangesAsync();

            var player = await service.GetPlayerAsync("User");

            Assert.NotNull(player);
            Assert.Equal("User", player.Name);
            Assert.Equal(15, player.Gold);
        }
    }


    public class CheckExistPlayer : UserServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenNameIsEmpty()
        {
            var service = new UserService(null!);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CheckExistPlayerAsync("")
            );
        }


        [Fact]
        public async Task ShouldCheckExistPlayer_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            context.Players.Add(new Player { Name = "User", PasswordHash = "..." });
            await context.SaveChangesAsync();

            bool exists = await service.CheckExistPlayerAsync("User");

            Assert.True(exists);
        }
    }


    public class SignIn : UserServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.SignInAsync("Ghost", "...");

            Assert.Null(result);
        }


        [Fact]
        public async Task ShouldThrow_WhenPasswordIsWrong()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            string hash = BCrypt.Net.BCrypt.HashPassword("correct");
            context.Players.Add(new Player { Name = "User", PasswordHash = hash });
            await context.SaveChangesAsync();

            var result = await service.SignInAsync("User", "Wrong");

            Assert.Null(result);
        }


        [Fact]
        public async Task ShouldReturnPlayer_WhenSuccessful()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            string correctPass = "qwerty";
            string hash = BCrypt.Net.BCrypt.HashPassword(correctPass);
            context.Players.Add(new Player { Name = "User", PasswordHash = hash });
            await context.SaveChangesAsync();

            var player = await service.SignInAsync("User", correctPass);

            Assert.NotNull(player);
            Assert.Equal("User", player.Name);
        }
    }


    public class Register : UserServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerAlreadyExists()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            context.Players.Add(new Player { Name = "Old User", PasswordHash = "..." });
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RegisterAsync("Old User", "...")
            );

            Assert.Contains("уже существует", ex.Message);
        }


        [Fact]
        public async Task ShouldRegister_WhenNewPlayer()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            var newPlayer = await service.RegisterAsync("New User", "qwerty123");

            var dbPlayer = await context.Players.FirstAsync(p => p.Name == "New User");

            Assert.NotNull(dbPlayer);
            Assert.Equal(10, dbPlayer.Gold);
            Assert.NotEqual("qwerty123", dbPlayer.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("qwerty123", dbPlayer.PasswordHash));
        }
    }


    public class GrantAdminRights : UserServiceTests
    {
        [Fact]
        public async Task ShouldThrow_WhenPlayerDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GrantAdminRightsAsync("newb", "Ghost", "...")
            );

            Assert.Contains("“екущий игрок не найден", ex.Message);
        }


        [Fact]
        public async Task ShouldThrow_WhenExpectedAdminDoesNotExist()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            context.Players.Add(new Player { Name = "Admin", PasswordHash = "..."});
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GrantAdminRightsAsync("ghost-newb", "Admin", "...")
            );

            Assert.Contains("не найден.", ex.Message);
        }


        [Fact]
        public async Task ShouldThrow_WhenCallerIsNotAdmin()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            var admin = new Player { Name = "Fake Admin", PasswordHash = "qwerty123" };
            var newb = new Player { Name = "newb", PasswordHash = "..." };
            context.Players.AddRange(admin, newb);
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GrantAdminRightsAsync("newb", "Fake Admin", "qwerty123")
            );

            Assert.Contains("нет привилегий посв€щени€", ex.Message);
        }


        [Fact]
        public async Task ShouldThrow_WhenPasswordIsWrong()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            var admin = new Player 
            { 
                Name = "Admin", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct123"), 
                IsAdmin = true 
            };
            var newb = new Player { Name = "newb", PasswordHash = "..." };
            context.Players.AddRange(admin, newb);
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GrantAdminRightsAsync("newb", "Admin", "wrong123")
            );

            Assert.Contains("Ќеверный пароль", ex.Message);
        }


        [Fact]
        public async Task ShouldGrantRights_WhenCallerIsAdminAndPassIsCorrec()
        {
            using var context = GetInMemoryContext();
            var service = new UserService(context);

            var admin = new Player
            {
                Name = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("qwerty123"),
                IsAdmin = true
            };
            var newb = new Player { Name = "New Admin", PasswordHash = "..." };
            context.Players.AddRange(admin, newb);
            await context.SaveChangesAsync();

            await service.GrantAdminRightsAsync("New Admin", "Admin", "qwerty123");

            var newAdmin = await context.Players.FirstAsync(p => p.Name == "New Admin");

            Assert.True(newAdmin.IsAdmin);
        }
    }
}