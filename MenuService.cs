using System.Linq;
using RestaurantManager.Models;

namespace RestaurantManager.Services
{
    // Menu urunlerini artik yerel Data/menu.json dosyasina degil, web
    // backend'ine (server.js) HTTP uzerinden okuyup yaziyor. Ayni menu, hem
    // Mac uygulamasinda hem web panelinde gorunur.
    public class MenuService
    {
        private readonly ApiClient _api;
        public List<MenuItem> Items { get; private set; } = new();

        public MenuService(ApiClient api)
        {
            _api = api;
        }

        // Sunucudaki guncel menuyu ceker (GET /api/menu).
        public async Task YukleAsync()
        {
            Items = await _api.GetAsync<List<MenuItem>>("api/menu");
        }

        public async Task<MenuItem> EkleAsync(MenuItem item)
        {
            var eklenen = await _api.PostAsync<MenuItem>("api/menu", new
            {
                name = item.Name,
                category = (int)item.Category,
                price = item.Price
            });
            Items.Add(eklenen);
            return eklenen;
        }

        public async Task GuncelleAsync(MenuItem item)
        {
            var guncellenen = await _api.PutAsync<MenuItem>($"api/menu/{item.Id}", new
            {
                name = item.Name,
                category = (int)item.Category,
                price = item.Price
            });

            var mevcut = Items.FirstOrDefault(x => x.Id == item.Id);
            if (mevcut != null)
            {
                mevcut.Name = guncellenen.Name;
                mevcut.Category = guncellenen.Category;
                mevcut.Price = guncellenen.Price;
            }
        }

        public async Task SilAsync(MenuItem item)
        {
            await _api.DeleteAsync($"api/menu/{item.Id}");
            Items.RemoveAll(x => x.Id == item.Id);

            // NOT: Bu urun daha once bir masaya siparis olarak eklenmisse,
            // o siparisin adi/fiyati sunucu tarafinda OrderLine icinde ayrica
            // saklandigi icin gecmis siparis kayitlari etkilenmez.
        }
    }
}
