namespace InventoryApp.Entities
{
    public class InventoryItem
    {
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;
        public int Amount { get; set; }
    }
}
