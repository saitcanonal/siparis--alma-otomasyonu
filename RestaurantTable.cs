namespace RestaurantManager.Models
{
    // Restorandaki tek bir masa.
    public class RestaurantTable
    {
        public int Number { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Bos;
        public int PersonCount { get; set; }
        public DateTime? SeatedAt { get; set; } // Müşterinin oturduğu saat
        public List<OrderLine> Orders { get; set; } = new();

        // Masanın o anki toplam hesabı
        public decimal GetTotal()
        {
            decimal toplam = 0;
            foreach (var order in Orders)
                toplam += order.Toplam;
            return toplam;
        }

        // Masada kaç dakikadır oturuluyor
        public int GetOturmaDakika()
        {
            if (SeatedAt == null) return 0;
            var fark = DateTime.Now - SeatedAt.Value;
            return (int)fark.TotalMinutes;
        }

        // Masayı tamamen boşalt / sıfırla (hesap kapatıldığında çağrılır)
        public void Bosalt()
        {
            Status = TableStatus.Bos;
            PersonCount = 0;
            SeatedAt = null;
            Orders.Clear();

            // TODO (devam noktası): Burada "Bosalt()" çağrılmadan önce
            // hesabı bir "GünlükSatışlar" listesine / dosyasına kaydedip
            // basit bir ciro raporu tutabilirsin. Şu an kapatılan hesap
            // bilgisi hiçbir yerde saklanmıyor, sadece sıfırlanıyor.
        }
    }
}
