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
    public partial class TableDetailWindow : Window
    {
        private readonly RestaurantTable _masa;
        private readonly MenuService _menuService;
        private readonly TableService _tableService;

        public TableDetailWindow(RestaurantTable masa, MenuService menuService, TableService tableService)
        {
            InitializeComponent();
            _masa = masa;
            _menuService = menuService;
            _tableService = tableService;

            CmbKategori.ItemsSource = Enum.GetValues(typeof(MenuCategory));
            CmbKategori.SelectedIndex = 0;

            Yenile();
        }

        private void Yenile()
        {
            TxtBaslik.Text = $"Masa {_masa.Number} - {_masa.Status}";
            TxtKisiSayisi.Text = _masa.PersonCount.ToString();

            TxtOturmaBilgisi.Text = _masa.SeatedAt == null
                ? "Henüz oturulmadı"
                : $"Oturma Saati: {_masa.SeatedAt:HH:mm}   |   Geçen Süre: {_masa.GetOturmaDakika()} dk";

            // Listeyi tazelemek için önce null atayıp sonra yeniden bağlıyoruz
            ListeSiparisler.ItemsSource = null;
            ListeSiparisler.ItemsSource = _masa.Orders;

            TxtGenelToplam.Text = $"Genel Toplam: {_masa.GetTotal():0.00} TL";

            UrunListesiniGuncelle();
        }

        private void CmbKategori_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UrunListesiniGuncelle();
        }

        private void UrunListesiniGuncelle()
        {
            if (CmbKategori.SelectedItem == null) return;
            var kategori = (MenuCategory)CmbKategori.SelectedItem;
            var urunListesi = _menuService.Items.Where(m => m.Category == kategori).ToList();
            CmbUrun.ItemsSource = urunListesi;
            if (urunListesi.Count > 0)
                CmbUrun.SelectedIndex = 0;
        }

        // "Masayı Aç (Dolu)" -> sunucuda masayı dolu işaretler ve oturma saatini kaydeder
        private async void BtnMasayiAc_Click(object? sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtKisiSayisi.Text, out int kisi) || kisi <= 0)
            {
                await SimpleDialogs.ShowInfoAsync(this, "Lütfen geçerli bir kişi sayısı girin.");
                return;
            }

            try
            {
                await _tableService.MasaAcAsync(_masa, kisi);
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"İşlem başarısız: {ex.Message}");
            }
        }

        // "Rezerve Et" -> sunucuda masayı rezervasyonlu işaretler
        private async void BtnRezerveEt_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await _tableService.RezerveEtAsync(_masa);
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"İşlem başarısız: {ex.Message}");
            }
        }

        // "Masayı Boşalt" -> sunucuda hesabı kapatır, masayı boş işaretler
        private async void BtnBosalt_Click(object? sender, RoutedEventArgs e)
        {
            bool onay = await SimpleDialogs.ShowConfirmAsync(
                this,
                "Masa boşaltılsın mı? Tüm sipariş bilgileri silinecek.",
                "Onay");

            if (!onay) return;

            try
            {
                await _tableService.BosaltAsync(_masa);
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"İşlem başarısız: {ex.Message}");
            }
        }

        // Yemek/içecek/tatlı ekleme butonu
        private async void BtnUrunEkle_Click(object? sender, RoutedEventArgs e)
        {
            if (CmbUrun.SelectedItem is not MenuItem secili)
            {
                await SimpleDialogs.ShowInfoAsync(this, "Lütfen bir ürün seçin.");
                return;
            }

            if (!int.TryParse(TxtAdet.Text, out int adet) || adet <= 0)
            {
                await SimpleDialogs.ShowInfoAsync(this, "Lütfen geçerli bir adet girin.");
                return;
            }

            try
            {
                await _tableService.SiparisEkleAsync(_masa, secili.Id, adet);
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"Ürün eklenemedi: {ex.Message}");
            }
        }

        // Yemek/içecek çıkarma butonu (seçili sipariş satırını siler)
        private async void BtnUrunCikar_Click(object? sender, RoutedEventArgs e)
        {
            if (ListeSiparisler.SelectedItem is not OrderLine secili)
            {
                await SimpleDialogs.ShowInfoAsync(this, "Çıkarmak için listeden bir ürün seçin.");
                return;
            }

            try
            {
                await _tableService.SiparisCikarAsync(_masa, secili.MenuItemId);
                Yenile();
            }
            catch (Exception ex)
            {
                await SimpleDialogs.ShowInfoAsync(this, $"Ürün çıkarılamadı: {ex.Message}");
            }
        }

        private void BtnKapat_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
