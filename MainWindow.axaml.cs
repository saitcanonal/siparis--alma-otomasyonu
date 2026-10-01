using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RestaurantManager.Models;
using RestaurantManager.Services;

namespace RestaurantManager
{
    public partial class MainWindow : Window
    {
        private readonly ApiClient _api = new(ApiConfig.BaseUrl);
        private readonly TableService _tableService;
        private readonly MenuService _menuService;
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _tableService = new TableService(_api);
            _menuService = new MenuService(_api);

            // Oturma sürelerinin (dakika) güncel görünmesi VE web panelinden
            // yapılan değişikliklerin (başka bir masa açılması, sipariş vb.)
            // görünmesi için masa kartları her 30 saniyede bir sunucudan
            // yeniden çekilip çizilir.
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _timer.Tick += async (s, e) => await YukleVeCizAsync();
            _timer.Start();

            _ = YukleVeCizAsync();
        }

        // Sunucudan (server.js) masa + menü verisini çeker ve ekranı çizer.
        private async Task YukleVeCizAsync()
        {
            try
            {
                await _tableService.YukleAsync();
                await _menuService.YukleAsync();
                MasalariCiz();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this,
                    $"Web sunucusuna bağlanılamadı ({ApiConfig.BaseUrl}).\n\n" +
                    "RestaurantManager-Web klasöründe \"node server.js\" komutunun " +
                    "çalıştığından emin ol.\n\n" +
                    $"Hata: {ex.Message}");
            }
        }

        private void MasalariCiz()
        {
            MasaPaneli.Children.Clear();

            foreach (var masa in _tableService.Tables.OrderBy(t => t.Number))
            {
                MasaPaneli.Children.Add(MasaKartiOlustur(masa));
            }

            TxtToplam.Text = $"Toplam Masa: {_tableService.Tables.Count}";
            TxtBos.Text = $"Boş: {_tableService.Tables.Count(t => t.Status == TableStatus.Bos)}";
            TxtDolu.Text = $"Dolu: {_tableService.Tables.Count(t => t.Status == TableStatus.Dolu)}";
            TxtRezerve.Text = $"Rezervasyonlu: {_tableService.Tables.Count(t => t.Status == TableStatus.Rezervasyonlu)}";
        }

        // Tek bir masa kartını (renkli kutu) oluşturur.
        private Border MasaKartiOlustur(RestaurantTable masa)
        {
            IBrush arkaPlan = masa.Status switch
            {
                TableStatus.Bos => Brushes.LightGreen,
                TableStatus.Dolu => Brushes.IndianRed,
                TableStatus.Rezervasyonlu => Brushes.Orange,
                _ => Brushes.LightGray
            };

            var panel = new StackPanel { Margin = new Thickness(6) };
            panel.Children.Add(new TextBlock
            {
                Text = $"Masa {masa.Number}",
                FontWeight = FontWeight.Bold,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = masa.Status.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            if (masa.Status == TableStatus.Dolu)
            {
                panel.Children.Add(new TextBlock { Text = $"Kişi: {masa.PersonCount}", HorizontalAlignment = HorizontalAlignment.Center });
                panel.Children.Add(new TextBlock { Text = $"Süre: {masa.GetOturmaDakika()} dk", HorizontalAlignment = HorizontalAlignment.Center });
                panel.Children.Add(new TextBlock { Text = $"Hesap: {masa.GetTotal():0.00} TL", HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeight.Bold });
            }

            var border = new Border
            {
                Width = 155,
                Height = 125,
                Margin = new Thickness(6),
                Background = arkaPlan,
                CornerRadius = new CornerRadius(10),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = panel,
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            // Karta tıklayınca masa detay ekranı açılır
            // (oturma saati, sipariş ekle/çıkar, boşalt/rezerve et vs. orada yapılır).
            border.PointerPressed += (s, e) => MasaDetayAc(masa);
            return border;
        }

        private async void MasaDetayAc(RestaurantTable masa)
        {
            var pencere = new TableDetailWindow(masa, _menuService, _tableService);
            await pencere.ShowDialog(this);
            MasalariCiz();
        }

        // "+ Masa Ekle" butonu: masa sayısını çoğaltır.
        private async void BtnYeniMasa_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await _tableService.YeniMasaEkleAsync();
                MasalariCiz();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"Masa eklenemedi: {ex.Message}");
            }
        }

        private async void BtnMenuYonetimi_Click(object? sender, RoutedEventArgs e)
        {
            var pencere = new MenuManagerWindow(_menuService);
            await pencere.ShowDialog(this);
            MasalariCiz();
        }

        // TODO (devam noktası): İstersen buraya bir "Gün Sonu Raporu" butonu
        // ekleyip, o an dolu olan masaların toplam hesaplarını toplayarak
        // basit bir ciro özeti gösterebilirsin.
    }
}
