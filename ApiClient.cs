using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace RestaurantManager.Services
{
    // Bu proje artik verileri yerel bir JSON dosyasina degil, ayni klasordeki
    // RestaurantManager-Web projesindeki Node.js sunucusuna (server.js)
    // HTTP uzerinden okuyup yaziyor. Boylece Mac uygulamasi ve web paneli
    // AYNI veriyi (Data/tables.json, Data/menu.json - sunucunun kendi
    // makinesinde) paylasir.
    //
    // Bu sinif, TableService ve MenuService'in ortak kullandigi kucuk bir
    // HTTP yardimcisidir: GET/POST/PUT/DELETE atar, JSON gövdeyi okur/yazar
    // ve sunucudan donen hata mesajlarini (ör. "Lütfen geçerli bir kişi
    // sayısı girin.") ApiException olarak fırlatır ki ekranda oldugu gibi
    // gosterilebilsin.
    public class ApiClient
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public ApiClient(string baseUrl)
        {
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<T> GetAsync<T>(string path)
        {
            var json = await _http.GetStringAsync(path);
            return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
        }

        public async Task<T> PostAsync<T>(string path, object? body = null)
        {
            var content = new StringContent(
                body == null ? "" : JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");
            var response = await _http.PostAsync(path, content);
            await FirlatEgerHataliAsync(response);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
        }

        public async Task<T> PutAsync<T>(string path, object body)
        {
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(path, content);
            await FirlatEgerHataliAsync(response);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
        }

        // Govdesi olan DELETE cevaplari icin (ör. siparis cikarinca guncel masa doner).
        public async Task<T> DeleteAsync<T>(string path)
        {
            var response = await _http.DeleteAsync(path);
            await FirlatEgerHataliAsync(response);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
        }

        // Govdesi olmayan/onemsiz DELETE cevaplari icin (ör. menuden urun silme -> 204).
        public async Task DeleteAsync(string path)
        {
            var response = await _http.DeleteAsync(path);
            await FirlatEgerHataliAsync(response);
        }

        private static async Task FirlatEgerHataliAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            string mesaj = $"Sunucu hatasi ({(int)response.StatusCode})";
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var hata))
                    mesaj = hata.GetString() ?? mesaj;
            }
            catch
            {
                // Govde JSON degilse ya da bosa, yukaridaki genel mesaj kullanilir.
            }

            throw new ApiException(mesaj);
        }
    }

    // TableService/MenuService'in yakalayip kullaniciya gosterdigi hata turu.
    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
    }
}
