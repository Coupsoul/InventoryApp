using System.Text.Json.Serialization;

namespace InventoryApp.DTOs
{
    internal class BtcPriceResponse
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public string Price { get; set; } = string.Empty;
    }
}