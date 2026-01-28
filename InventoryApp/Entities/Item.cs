using InventoryApp.Enums;

namespace InventoryApp.Entities
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int Price { get; set; }
        public Currency PriceCurrency { get; set; }
        public List<InventoryItem> InventoryItems { get; set; } = new();

        private Item() { }
        public Item(string name, Currency priceCurrency, int price, string? description = null)
        {
            Name = name;
            PriceCurrency = priceCurrency;
            Price = price;
            Description = description;
        }

        public int GetSellPrice()
        {
            return Price / 2;
        }
    }
}
