using InventoryApp.Entities;

namespace InventoryApp.Services.Interfaces
{
    public interface IUserService
    {
        Task<Player?> GetPlayerAsync(string name);

        Task<bool> CheckExistPlayerAsync(string name);

        Task<Player?> SignInAsync(string name, string password);

        Task<Player> RegisterAsync(string name, string password);

        Task GrantAdminRightsAsync(string newAdminName, string playerName, string password);
    }
}