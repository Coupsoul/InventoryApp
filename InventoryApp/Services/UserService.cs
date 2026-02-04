using InventoryApp.Data;
using InventoryApp.Entities;
using InventoryApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationContext _context;

        public UserService(ApplicationContext context)
        {
            _context = context;
        }


        public async Task<Player?> GetPlayerAsync(string name)
        {
            FastFailArgument(name, "Имя не может быть пустым.");

            return await _context.Players.FirstOrDefaultAsync(p => p.Name == name);
        }


        public async Task<bool> CheckExistPlayerAsync(string name)
        {
            FastFailArgument(name, "Имя не может быть пустым.");

            return await _context.Players.AnyAsync(p => p.Name == name);
        }


        public async Task<Player?> SignInAsync(string name, string password)
        {
            FastFailArgument(name, "Имя не может быть пустым.");
            FastFailArgument(password, "Пароль не может быть пустым.");

            var player = await GetPlayerAsync(name);
            if (player == null) return null;

            bool isValid = BCrypt.Net.BCrypt.Verify(password, player.PasswordHash);
            return isValid ? player : null;
        }


        public async Task<Player> RegisterAsync(string name, string password)
        {
            FastFailArgument(name, "Имя не может быть пустым.");
            FastFailArgument(password, "Пароль не может быть пустым.");
            if (await CheckExistPlayerAsync(name))
                throw new InvalidOperationException($"Игрок с именем {name} уже существует.");

            var player = new Player
            {
                Name = name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Gold = 10,
                Gems = 0
            };
            
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }


        public async Task GrantAdminRightsAsync(string newAdminName, string playerName, string password)
        {
            FastFailArgument(newAdminName, "Имя нового админа не указано.");
            FastFailArgument(playerName, "Имя не может быть пустым.");
            FastFailArgument(password, "Пароль не может быть пустым.");

            var player = await GetPlayerAsync(playerName) 
                ?? throw new InvalidOperationException("Текущий игрок не найден.");
            var newAdmin = await GetPlayerAsync(newAdminName)
                ?? throw new InvalidOperationException($"Игрок {newAdminName} не найден.");

            if (!BCrypt.Net.BCrypt.Verify(password, player.PasswordHash))
                throw new ArgumentException("Неверный пароль.");

            if (!player.IsAdmin)
                throw new InvalidOperationException("У вас нет прав назначать администраторов.");

            newAdmin.IsAdmin = true;

            await _context.SaveChangesAsync();
        }


        private void FastFailArgument(string arg, string message)
        {
            if (string.IsNullOrWhiteSpace(arg))
                throw new ArgumentException(message, nameof(arg));
        }
    }
}