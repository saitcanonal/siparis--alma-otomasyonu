# Restoran Paneli — Web Backend + Frontend

Bu proje, gönderdiğin **RestaurantManager** masaüstü uygulamasının (Avalonia/.NET)
`TableService`, `MenuService`, `RestaurantTable`, `MenuItem` ve `OrderLine`
mantığını birebir koruyarak, aynı iş kurallarıyla çalışan bir **web backend
(Node.js)** ve ona bağlı görsel bir **panel arayüzü (HTML/CSS/JS)** olarak
yeniden hazırlanmış halidir.

## Nasıl çalıştırılır

Hiçbir üçüncü parti pakete ihtiyaç yoktur (sadece Node.js'in kendi
modülleri kullanılır).

```bash
node server.js
```

Sonra tarayıcıda şu adresi aç:

```
http://localhost:3000
```

Port'u değiştirmek istersen: `PORT=8080 node server.js`

## Klasör yapısı

```
RestaurantManager-Web/
├── server.js         Backend: REST API + statik dosya sunucusu
├── public/
│   ├── index.html     Panel arayüzü (masa planı, modallar)
│   ├── styles.css      Görsel tasarım
│   └── app.js          Arayüz mantığı (fetch ile API'ye bağlanır)
├── Data/
│   ├── tables.json     Masa verileri (otomatik oluşturulur)
│   └── menu.json        Menü verileri (otomatik oluşturulur)
└── package.json
```

## Orijinal koddan neyin birebir taşındığı

| Orijinal (C#)                          | Web karşılığı                                 |
|-----------------------------------------|-----------------------------------------------|
| `TableService` (Data/tables.json)       | `server.js` içindeki masa fonksiyonları        |
| `MenuService` (Data/menu.json)          | `server.js` içindeki menü fonksiyonları        |
| `TableStatus` enum (Bos/Dolu/Rezervasyonlu) | Aynı sıradaki 0/1/2 değerleri              |
| `MenuCategory` enum (Yemek/Icecek/Tatli) | Aynı sıradaki 0/1/2 değerleri                |
| `RestaurantTable.GetTotal()`            | `Total` alanı (API yanıtında hazır hesaplanır) |
| `RestaurantTable.GetOturmaDakika()`     | `OturmaDakika` alanı                           |
| `RestaurantTable.Bosalt()`              | `POST /api/tables/:no/close`                   |
| `MainWindow` (masa kartları + istatistik) | Ana ekran + üst bilgi çubuğu                 |
| `TableDetailWindow` (aç/rezerve et/ürün ekle-çıkar) | "Masa Detay" modalı              |
| `MenuManagerWindow` (ekle/güncelle/sil) | "Menü Yönetimi" modalı                        |
| 30 saniyede bir otomatik yenileme (`DispatcherTimer`) | `setInterval(drawTables, 30000)` |

Başlangıç verileri de birebir aynıdır: 15 boş masa ve orijinal `MenuService`
içindeki 10 kalem başlangıç menüsü (Izgara Köfte, Tavuk Şiş, Kola, Baklava vb.).

## API uç noktaları

| Yöntem | Yol                                   | Açıklama                         |
|--------|----------------------------------------|-----------------------------------|
| GET    | `/api/tables`                          | Tüm masalar + özet istatistik     |
| POST   | `/api/tables`                          | Yeni masa ekle                    |
| GET    | `/api/tables/:no`                      | Tek masa detayı                   |
| POST   | `/api/tables/:no/open`                 | Masayı aç `{ personCount }`       |
| POST   | `/api/tables/:no/reserve`              | Rezerve et                        |
| POST   | `/api/tables/:no/close`                | Masayı boşalt                     |
| POST   | `/api/tables/:no/orders`               | Ürün ekle `{ menuItemId, adet }`  |
| DELETE | `/api/tables/:no/orders/:menuItemId`   | Ürünü çıkar                       |
| GET    | `/api/menu`                            | Menü listesi                      |
| POST   | `/api/menu`                            | Ürün ekle `{ name, category, price }` |
| PUT    | `/api/menu/:id`                        | Ürün güncelle                     |
| DELETE | `/api/menu/:id`                        | Ürün sil                          |

## Notlar

- Veriler `Data/tables.json` ve `Data/menu.json` dosyalarına, orijinal
  uygulamadaki gibi her işlemden sonra kaydedilir.
- Bir ürün menüden silinse bile, daha önce bir masaya eklenmiş siparişin
  adı/fiyatı `OrderLine` içinde ayrıca saklandığından geçmiş sipariş
  etkilenmez (orijinal davranışla aynı).
- Orijinal projede olmayıp burada eklenen tek şey görsel tasarımdır;
  iş mantığında hiçbir kural değiştirilmedi veya eklenmedi.
