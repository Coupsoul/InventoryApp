using System.ComponentModel.DataAnnotations;
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
        public Item(string name, Currency currency, int price, string? description = null)
        {
            Name = name;
            PriceCurrency = currency;
            Price = price;
            Description = description;
        }
    }
}
