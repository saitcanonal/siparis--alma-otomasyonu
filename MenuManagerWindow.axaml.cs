using Avalonia.Controls;
using Avalonia.Interactivity;
using RestaurantManager.Models;
using RestaurantManager.Services;
// Avalonia.Controls içinde de "MenuItem" adında bir sınıf (menü çubuğu öğesi)
// olduğu için, buradaki "MenuItem" adının her zaman bizim modelimizi
// göstermesi için takma ad (alias) tanımlıyoruz.
using MenuItem = RestaurantManager.Models.MenuItem;

namespace RestaurantManager
{
    public partial class MenuManagerWindow : Window
    {
        private readonly MenuService _menuService;
        private MenuItem? _seciliUrun;

        public MenuManagerWindow(MenuService menuService)
        {
            InitializeComponent();
            _menuService = menuService;

            CmbKategori.ItemsSource = Enum.GetValues(typeof(MenuCategory));
            CmbKategori.SelectedIndex = 0;

            Yenile();
        }

        private void Yenile()
        {
            ListeMenu.ItemsSource = null;
            ListeMenu.ItemsSource = _menuService.Items;
        }

        private void ListeMenu_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ListeMenu.SelectedItem is not MenuItem secili) return;

            _seciliUrun = secili;
            TxtAd.Text = secili.Name;
            CmbKategori.SelectedItem = secili.Category;
            TxtFiyat.Text = secili.Price.ToString();
        }

        private async Task<(bool gecerli, decimal fiyat)> FormGecerliMiAsync()
        {
            if (string.IsNullOrWhiteSpace(TxtAd.Text))
            {
                await SimpleDialogs.ShowInfoAsync(this, "Lütfen ürün adı girin.");
                return (false, 0);
            }
            if (!decimal.TryParse(TxtFiyat.Text, out decimal fiyat) || fiyat < 0)
            {
                await SimpleDialogs.ShowInfoAsync(this, "Lütfen geçerli bir fiyat girin.");
                return (false, 0);
            }
            return (true, fiyat);
        }

        // Yeni yemek/içecek/tatlı ekleme -> POST /api/menu
        private async void BtnEkle_Click(object? sender, RoutedEventArgs e)
        {
            var (gecerli, fiyat) = await FormGecerliMiAsync();
            if (!gecerli) return;

            try
            {
                await _menuService.EkleAsync(new MenuItem
                {
                    Name = TxtAd.Text!.Trim(),
                    Category = (MenuCategory)CmbKategori.SelectedItem!,
                    Price = fiyat
                });
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"Ürün eklenemedi: {ex.Message}");
            }
        }

        // Seçili ürünün fiyatını / adını / kategorisini güncelleme -> PUT /api/menu/:id
        private async void BtnGuncelle_Click(object? sender, RoutedEventArgs e)
        {
            if (_seciliUrun == null)
            {
                await SimpleDialogs.ShowInfoAsync(this, "Güncellemek için listeden bir ürün seçin.");
                return;
            }

            var (gecerli, fiyat) = await FormGecerliMiAsync();
            if (!gecerli) return;

            _seciliUrun.Name = TxtAd.Text!.Trim();
            _seciliUrun.Category = (MenuCategory)CmbKategori.SelectedItem!;
            _seciliUrun.Price = fiyat;

            try
            {
                await _menuService.GuncelleAsync(_seciliUrun);
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"Ürün güncellenemedi: {ex.Message}");
            }
        }

        // Menüden ürün çıkarma -> DELETE /api/menu/:id
        private async void BtnSil_Click(object? sender, RoutedEventArgs e)
        {
            if (_seciliUrun == null)
            {
                await SimpleDialogs.ShowInfoAsync(this, "Silmek için listeden bir ürün seçin.");
                return;
            }

            try
            {
                await _menuService.SilAsync(_seciliUrun);
                _seciliUrun = null;
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"Ürün silinemedi: {ex.Message}");
            }
        }

        private void BtnKapat_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
