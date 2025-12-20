namespace InventoryApp.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Gold { get; set; }
        public int Gems { get; set; }
        public List<InventoryItem> Inventory { get; set; } = new();
    }
}
