namespace RestaurantManager.Services
{
    // Web backend'in (RestaurantManager-Web / server.js) hangi adreste
    // calistigini burada ayarliyoruz.
    //
    // Varsayilan: ayni bilgisayarda "node server.js" ile calistirilan sunucu
    // (http://localhost:3000/).
    //
    // Sunucuyu baska bir bilgisayarda / ag uzerinde calistirirsan, uygulamayi
    // acmadan once RESTAURANT_API_URL ortam degiskenini ayarlayabilirsin:
    //
    //   macOS/Linux terminalde:
    //     RESTAURANT_API_URL=http://192.168.1.20:3000/ dotnet run
    //
    //   VS Code'da .vscode/launch.json icine de "env" olarak eklenebilir.
    public static class ApiConfig
    {
        public static string BaseUrl =>
            Environment.GetEnvironmentVariable("RESTAURANT_API_URL")?.Trim() is { Length: > 0 } deger
                ? deger
                : "http://localhost:3000/";
    }
}
