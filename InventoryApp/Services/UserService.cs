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
            return await _context.Players.FirstOrDefaultAsync(p => p.Name == name);
        }


        public async Task<bool> CheckExistPlayerAsync(string name)
        {
            return await _context.Players.AnyAsync(p => p.Name == name);
        }


        public async Task<Player?> SignInAsync(string name, string password)
        {
            var player = await GetPlayerAsync(name);
            if (player == null) return null;

            bool isValid = BCrypt.Net.BCrypt.Verify(password, player.PasswordHash);
            return isValid ? player : null;
        }


        public async Task<Player> RegisterAsync(string name, string password)
        {
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


        public async Task<bool> GrantAdminRightsAsync(string newAdminName, string playerName, string password)
        {
            var player = await GetPlayerAsync(playerName); if (player == null) return false;
            var newAdmin = await GetPlayerAsync(newAdminName); if (newAdmin == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(password, player.PasswordHash))
                return false;

            newAdmin.IsAdmin = true;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}