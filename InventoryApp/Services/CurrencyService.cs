using System.Globalization;
using System.Net.Http.Json;
using InventoryApp.DTOs;
using Microsoft.Extensions.Configuration;

namespace InventoryApp.Services
{
    public class CurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public CurrencyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiUrl = configuration["BinanceApiUrl"] ?? throw new Exception("URL API не найден в appsettings.json.");
        }


        public async Task<int> GetGemPriceInGoldAsync()
        {
            decimal btcPrice = await GetRawBtcPriceAsync();

            if (btcPrice <= 0) return 80;

            return (int)(btcPrice / 1000);
        }


        private async Task<decimal> GetRawBtcPriceAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<BtcPriceResponse>(_apiUrl);
                if (response is not null && decimal.TryParse(response.Price, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                {
                    return price;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Лог: Не удалось получить курс. Причина: {ex.Message}]");
                return 0;
            }
        }
    }
}