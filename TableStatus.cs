namespace RestaurantManager.Models
{
    // Bir masanın anlık durumu
    public enum TableStatus
    {
        Bos,            // Yeşil - müşteri yok
        Dolu,           // Kırmızı - müşteri oturuyor
        Rezervasyonlu   // Turuncu - rezerve edilmiş
    }
}
