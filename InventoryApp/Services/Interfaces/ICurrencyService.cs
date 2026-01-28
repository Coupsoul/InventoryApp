namespace InventoryApp.Services.Interfaces
{
    public interface ICurrencyService
    {
        Task<int> GetGemPriceInGoldAsync();
    }
}