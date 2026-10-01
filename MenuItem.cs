namespace RestaurantManager.Models
{
    // Menüdeki tek bir ürün: yemek, içecek veya tatlı.
    // Fiyatlar istenildiği zaman MenuManagerWindow üzerinden değiştirilebilir.
    public class MenuItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public MenuCategory Category { get; set; }
        public decimal Price { get; set; }

        public override string ToString()
        {
            return $"{Name} - {Price:0.00} TL";
        }
    }
}
