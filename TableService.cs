using System.Linq;
using RestaurantManager.Models;

namespace RestaurantManager.Services
{
    // Masalari artik yerel Data/tables.json dosyasina degil, web backend'ine
    // (server.js) HTTP uzerinden okuyup yaziyor. Boylece Mac uygulamasi ve
    // web paneli ayni veriyi paylasir: biri masayi doldurunca digeri de
    // (yenilendiginde) bunu gorur.
    public class TableService
    {
        private readonly ApiClient _api;
        public List<RestaurantTable> Tables { get; private set; } = new();

        public TableService(ApiClient api)
        {
            _api = api;
        }

        // Sunucudaki guncel masa listesini ceker (GET /api/tables).
        // Program acilirken ve MainWindow'daki 30 saniyelik zamanlayicida cagrilir.
        public async Task YukleAsync()
        {
            var yanit = await _api.GetAsync<TablesResponse>("api/tables");
            Tables = yanit.Tables ?? new List<RestaurantTable>();
        }

        // "+ Masa Ekle" butonu -> POST /api/tables
        public async Task<RestaurantTable> YeniMasaEkleAsync()
        {
            var masa = await _api.PostAsync<RestaurantTable>("api/tables");
            Tables.Add(masa);
            return masa;
        }

        // "Masayi Ac (Dolu)" -> POST /api/tables/:no/open
        public async Task MasaAcAsync(RestaurantTable masa, int kisiSayisi)
        {
            var guncel = await _api.PostAsync<RestaurantTable>(
                $"api/tables/{masa.Number}/open",
                new { personCount = kisiSayisi });
            Uygula(masa, guncel);
        }

        // "Rezerve Et" -> POST /api/tables/:no/reserve
        public async Task RezerveEtAsync(RestaurantTable masa)
        {
            var guncel = await _api.PostAsync<RestaurantTable>($"api/tables/{masa.Number}/reserve");
            Uygula(masa, guncel);
        }

        // "Masayi Bosalt" -> POST /api/tables/:no/close
        public async Task BosaltAsync(RestaurantTable masa)
        {
            var guncel = await _api.PostAsync<RestaurantTable>($"api/tables/{masa.Number}/close");
            Uygula(masa, guncel);
        }

        // Siparise urun ekle -> POST /api/tables/:no/orders
        public async Task SiparisEkleAsync(RestaurantTable masa, Guid menuItemId, int adet)
        {
            var guncel = await _api.PostAsync<RestaurantTable>(
                $"api/tables/{masa.Number}/orders",
                new { menuItemId = menuItemId.ToString(), adet });
            Uygula(masa, guncel);
        }

        // Siparisten urun cikar -> DELETE /api/tables/:no/orders/:menuItemId
        public async Task SiparisCikarAsync(RestaurantTable masa, Guid menuItemId)
        {
            var guncel = await _api.DeleteAsync<RestaurantTable>($"api/tables/{masa.Number}/orders/{menuItemId}");
            Uygula(masa, guncel);
        }

        // Sunucudan gelen guncel bilgiyi, ekranin (TableDetailWindow'un) elinde
        // tuttugu AYNI RestaurantTable nesnesine, referansi degistirmeden isler.
        private static void Uygula(RestaurantTable hedef, RestaurantTable kaynak)
        {
            hedef.Status = kaynak.Status;
            hedef.PersonCount = kaynak.PersonCount;
            hedef.SeatedAt = kaynak.SeatedAt;
            hedef.Orders = kaynak.Orders;
        }
    }

    // GET /api/tables yanitindaki disaridaki zarf: { tables: [...], stats: {...} }
    public class TablesResponse
    {
        public List<RestaurantTable> Tables { get; set; } = new();
        public TableStats? Stats { get; set; }
    }

    public class TableStats
    {
        public int Total { get; set; }
        public int Bos { get; set; }
        public int Dolu { get; set; }
        public int Rezervasyonlu { get; set; }
    }
}
