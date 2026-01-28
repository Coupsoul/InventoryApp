namespace InventoryApp.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public int Gold { get; set; }
        public int Gems { get; set; }
        public bool IsAdmin { get; set; } = false;
        public List<InventoryItem> Inventory { get; set; } = new();
    }
}
