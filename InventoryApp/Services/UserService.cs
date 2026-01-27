using InventoryApp.Data;
using InventoryApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Services
{
    public class UserService
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


        public async 
    }
}
