using Avalonia;

namespace RestaurantManager
{
    internal class Program
    {
        // Programın başladığı ilk yer (WPF'teki otomatik oluşturulan
        // App.g.cs / Main yerine Avalonia'da bunu elle yazıyoruz).
        [STAThread]
        public static void Main(string[] args)
            => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
