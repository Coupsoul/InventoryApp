namespace InventoryApp.Entities
{
    public class Player
    {
        public const int MaxGold = 99999;
        public const int MaxGems = 99999;
        private int _gold;
        private int _gems;
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public int Gold 
        { 
            get => _gold;
            set => _gold = Math.Clamp(value, 0, MaxGold);
        }
        public int Gems 
        { 
            get => _gems;
            set => _gems = Math.Clamp(value, 0, MaxGems);
        }
        public bool IsAdmin { get; set; } = false;
        public List<InventoryItem> Inventory { get; set; } = new();
    }
}
