namespace RestaurantManager.Models
{
    // Bir masaya eklenen tek bir sipariş satırı (örn: 2 adet Izgara Köfte).
    // Menü fiyatı sonradan değişse bile geçmiş siparişin fiyatı bozulmasın
    // diye UnitPrice burada ayrıca saklanır (sipariş anındaki fiyat).
    public class OrderLine
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public MenuCategory Category { get; set; }
        public decimal UnitPrice { get; set; }
        public int Adet { get; set; }

        public decimal Toplam => UnitPrice * Adet;
    }
}
