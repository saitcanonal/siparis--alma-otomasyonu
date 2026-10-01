// server.js
// -----------------------------------------------------------------------
// RestaurantManager - Web Backend
//
// Bu dosya, orijinal masaüstü uygulamasındaki (Avalonia/C#) TableService ve
// MenuService sınıflarının mantığını birebir Node.js'e taşır:
//   - Masalar (RestaurantTable)  -> Data/tables.json
//   - Menü ürünleri (MenuItem)   -> Data/menu.json
//   - Sipariş satırları (OrderLine) her masanın "Orders" dizisinde tutulur
//
// Enum değerleri, C# tarafındaki numaralandırmayla AYNI sırada tutulur:
//   TableStatus:  0 = Bos, 1 = Dolu, 2 = Rezervasyonlu
//   MenuCategory: 0 = Yemek, 1 = Icecek, 2 = Tatli
//
// Harici bir paket GEREKMEZ (sadece Node.js'in kendi http/fs modülleri
// kullanılır) -> "npm install" yapmadan doğrudan "node server.js" ile çalışır.
// -----------------------------------------------------------------------

const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { URL } = require('url');

const PORT = process.env.PORT || 3000;
const DATA_DIR = path.join(__dirname, 'Data');
const TABLES_FILE = path.join(DATA_DIR, 'tables.json');
const MENU_FILE = path.join(DATA_DIR, 'menu.json');
const PUBLIC_DIR = path.join(__dirname, 'public');

const TableStatus = { Bos: 0, Dolu: 1, Rezervasyonlu: 2 };
const MenuCategory = { Yemek: 0, Icecek: 1, Tatli: 2 };

// ------------------------------------------------------------------
// Kalıcı depolama (TableService / MenuService karşılığı)
// ------------------------------------------------------------------

function ensureDataDir() {
  if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });
}

function defaultMenu() {
  // MenuService.Yukle() içindeki başlangıç menüsüyle birebir aynı.
  return [
    { Id: crypto.randomUUID(), Name: 'Izgara Köfte', Category: MenuCategory.Yemek, Price: 220 },
    { Id: crypto.randomUUID(), Name: 'Tavuk Şiş', Category: MenuCategory.Yemek, Price: 210 },
    { Id: crypto.randomUUID(), Name: 'Mercimek Çorbası', Category: MenuCategory.Yemek, Price: 90 },
    { Id: crypto.randomUUID(), Name: 'Karışık Pizza', Category: MenuCategory.Yemek, Price: 260 },
    { Id: crypto.randomUUID(), Name: 'Kola', Category: MenuCategory.Icecek, Price: 60 },
    { Id: crypto.randomUUID(), Name: 'Ayran', Category: MenuCategory.Icecek, Price: 40 },
    { Id: crypto.randomUUID(), Name: 'Su', Category: MenuCategory.Icecek, Price: 20 },
    { Id: crypto.randomUUID(), Name: 'Türk Kahvesi', Category: MenuCategory.Icecek, Price: 70 },
    { Id: crypto.randomUUID(), Name: 'Baklava', Category: MenuCategory.Tatli, Price: 130 },
    { Id: crypto.randomUUID(), Name: 'Sütlaç', Category: MenuCategory.Tatli, Price: 100 },
  ];
}

function defaultTables(baslangicMasaSayisi = 15) {
  // TableService.Yukle() ile aynı: başlangıçta 15 boş masa.
  const tables = [];
  for (let i = 1; i <= baslangicMasaSayisi; i++) {
    tables.push({
      Number: i,
      Status: TableStatus.Bos,
      PersonCount: 0,
      SeatedAt: null,
      Orders: [],
    });
  }
  return tables;
}

function loadJson(filePath, fallbackFactory) {
  ensureDataDir();
  if (fs.existsSync(filePath)) {
    try {
      const raw = fs.readFileSync(filePath, 'utf-8');
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) return parsed;
    } catch (e) {
      console.error(`Uyari: ${filePath} okunamadi, varsayilan veri kullaniliyor.`, e.message);
    }
  }
  const data = fallbackFactory();
  saveJson(filePath, data);
  return data;
}

function saveJson(filePath, data) {
  ensureDataDir();
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf-8');
}

let tables = loadJson(TABLES_FILE, () => defaultTables(15));
let menuItems = loadJson(MENU_FILE, defaultMenu);

function saveTables() { saveJson(TABLES_FILE, tables); }
function saveMenu() { saveJson(MENU_FILE, menuItems); }

// ------------------------------------------------------------------
// RestaurantTable yardımcı hesaplamalari (GetTotal / GetOturmaDakika)
// ------------------------------------------------------------------

function getTableTotal(masa) {
  return masa.Orders.reduce((toplam, o) => toplam + o.UnitPrice * o.Adet, 0);
}

function getOturmaDakika(masa) {
  if (!masa.SeatedAt) return 0;
  const fark = Date.now() - new Date(masa.SeatedAt).getTime();
  return Math.floor(fark / 60000);
}

function serializeTable(masa) {
  return {
    ...masa,
    Orders: masa.Orders.map(o => ({ ...o, Toplam: o.UnitPrice * o.Adet })),
    Total: getTableTotal(masa),
    OturmaDakika: getOturmaDakika(masa),
  };
}

// ------------------------------------------------------------------
// Küçük HTTP yardımcıları
// ------------------------------------------------------------------

function sendJson(res, status, payload) {
  const body = JSON.stringify(payload);
  res.writeHead(status, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': Buffer.byteLength(body),
    'Access-Control-Allow-Origin': '*',
  });
  res.end(body);
}

function sendError(res, status, message) {
  sendJson(res, status, { error: message });
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let chunks = [];
    req.on('data', c => chunks.push(c));
    req.on('end', () => {
      if (chunks.length === 0) return resolve({});
      try {
        resolve(JSON.parse(Buffer.concat(chunks).toString('utf-8')));
      } catch (e) {
        reject(new Error('Gecersiz JSON govde'));
      }
    });
    req.on('error', reject);
  });
}

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.json': 'application/json; charset=utf-8',
};

function serveStatic(req, res, urlPath) {
  let rel = urlPath === '/' ? '/index.html' : urlPath;
  const filePath = path.join(PUBLIC_DIR, rel);
  // Dizin disina cikilmasini engelle
  if (!filePath.startsWith(PUBLIC_DIR)) {
    res.writeHead(403); res.end('Forbidden'); return;
  }
  fs.readFile(filePath, (err, data) => {
    if (err) { res.writeHead(404); res.end('Not found'); return; }
    const ext = path.extname(filePath);
    res.writeHead(200, { 'Content-Type': MIME[ext] || 'application/octet-stream' });
    res.end(data);
  });
}

// ------------------------------------------------------------------
// Rotalar
// ------------------------------------------------------------------

const server = http.createServer(async (req, res) => {
  const parsed = new URL(req.url, `http://${req.headers.host}`);
  const segs = parsed.pathname.split('/').filter(Boolean); // örn ['api','tables','3','orders']

  if (req.method === 'OPTIONS') {
    res.writeHead(204, {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
    });
    return res.end();
  }

  if (segs[0] !== 'api') {
    return serveStatic(req, res, parsed.pathname);
  }

  try {
    // ---------------- /api/tables ----------------
    if (segs[1] === 'tables') {
      // GET /api/tables  -> tüm masalar (MasalariCiz karşılığı)
      if (segs.length === 2 && req.method === 'GET') {
        const list = [...tables].sort((a, b) => a.Number - b.Number).map(serializeTable);
        return sendJson(res, 200, {
          tables: list,
          stats: {
            total: tables.length,
            bos: tables.filter(t => t.Status === TableStatus.Bos).length,
            dolu: tables.filter(t => t.Status === TableStatus.Dolu).length,
            rezervasyonlu: tables.filter(t => t.Status === TableStatus.Rezervasyonlu).length,
          },
        });
      }

      // POST /api/tables -> YeniMasaEkle
      if (segs.length === 2 && req.method === 'POST') {
        const yeniNo = tables.length === 0 ? 1 : Math.max(...tables.map(t => t.Number)) + 1;
        const masa = { Number: yeniNo, Status: TableStatus.Bos, PersonCount: 0, SeatedAt: null, Orders: [] };
        tables.push(masa);
        saveTables();
        return sendJson(res, 201, serializeTable(masa));
      }

      const number = parseInt(segs[2], 10);
      const masa = tables.find(t => t.Number === number);

      if (segs.length === 3 && req.method === 'GET') {
        if (!masa) return sendError(res, 404, 'Masa bulunamadi.');
        return sendJson(res, 200, serializeTable(masa));
      }

      if (!masa) return sendError(res, 404, 'Masa bulunamadi.');

      // POST /api/tables/:n/open -> masayi ac (Dolu)
      if (segs[3] === 'open' && req.method === 'POST') {
        const body = await readBody(req);
        const kisi = parseInt(body.personCount, 10);
        if (!Number.isFinite(kisi) || kisi <= 0) {
          return sendError(res, 400, 'Lütfen geçerli bir kişi sayısı girin.');
        }
        masa.PersonCount = kisi;
        masa.Status = TableStatus.Dolu;
        masa.SeatedAt = new Date().toISOString();
        saveTables();
        return sendJson(res, 200, serializeTable(masa));
      }

      // POST /api/tables/:n/reserve -> rezerve et
      if (segs[3] === 'reserve' && req.method === 'POST') {
        masa.Status = TableStatus.Rezervasyonlu;
        saveTables();
        return sendJson(res, 200, serializeTable(masa));
      }

      // POST /api/tables/:n/close -> Bosalt()
      if (segs[3] === 'close' && req.method === 'POST') {
        masa.Status = TableStatus.Bos;
        masa.PersonCount = 0;
        masa.SeatedAt = null;
        masa.Orders = [];
        saveTables();
        return sendJson(res, 200, serializeTable(masa));
      }

      // POST /api/tables/:n/orders -> urun ekle
      if (segs[3] === 'orders' && segs.length === 4 && req.method === 'POST') {
        const body = await readBody(req);
        const menuItem = menuItems.find(m => m.Id === body.menuItemId);
        if (!menuItem) return sendError(res, 400, 'Lütfen bir ürün seçin.');
        const adet = parseInt(body.adet, 10);
        if (!Number.isFinite(adet) || adet <= 0) {
          return sendError(res, 400, 'Lütfen geçerli bir adet girin.');
        }
        const mevcut = masa.Orders.find(o => o.MenuItemId === menuItem.Id);
        if (mevcut) {
          mevcut.Adet += adet;
        } else {
          masa.Orders.push({
            MenuItemId: menuItem.Id,
            MenuItemName: menuItem.Name,
            Category: menuItem.Category,
            UnitPrice: menuItem.Price,
            Adet: adet,
          });
        }
        saveTables();
        return sendJson(res, 200, serializeTable(masa));
      }

      // DELETE /api/tables/:n/orders/:menuItemId -> urun cikar
      if (segs[3] === 'orders' && segs.length === 5 && req.method === 'DELETE') {
        const menuItemId = segs[4];
        const before = masa.Orders.length;
        masa.Orders = masa.Orders.filter(o => o.MenuItemId !== menuItemId);
        if (masa.Orders.length === before) {
          return sendError(res, 404, 'Sipariş satırı bulunamadı.');
        }
        saveTables();
        return sendJson(res, 200, serializeTable(masa));
      }
    }

    // ---------------- /api/menu ----------------
    if (segs[1] === 'menu') {
      if (segs.length === 2 && req.method === 'GET') {
        return sendJson(res, 200, menuItems);
      }

      if (segs.length === 2 && req.method === 'POST') {
        const body = await readBody(req);
        const check = validateMenuInput(body);
        if (!check.ok) return sendError(res, 400, check.message);
        const item = { Id: crypto.randomUUID(), Name: body.name.trim(), Category: check.category, Price: check.price };
        menuItems.push(item);
        saveMenu();
        return sendJson(res, 201, item);
      }

      const id = segs[2];
      const item = menuItems.find(m => m.Id === id);

      if (segs.length === 3 && req.method === 'PUT') {
        if (!item) return sendError(res, 404, 'Ürün bulunamadı.');
        const body = await readBody(req);
        const check = validateMenuInput(body);
        if (!check.ok) return sendError(res, 400, check.message);
        item.Name = body.name.trim();
        item.Category = check.category;
        item.Price = check.price;
        saveMenu();
        return sendJson(res, 200, item);
      }

      if (segs.length === 3 && req.method === 'DELETE') {
        if (!item) return sendError(res, 404, 'Ürün bulunamadı.');
        // NOT: Bu ürün daha önce bir masaya sipariş olarak eklenmişse,
        // o siparişin adı/fiyatı OrderLine içinde ayrıca saklandığı için
        // geçmiş sipariş kayıtları etkilenmez (orijinal davranışla aynı).
        menuItems = menuItems.filter(m => m.Id !== id);
        saveMenu();
        return sendJson(res, 204, {});
      }
    }

    return sendError(res, 404, 'Bulunamadı.');
  } catch (e) {
    console.error(e);
    return sendError(res, 500, 'Sunucu hatası: ' + e.message);
  }
});

function validateMenuInput(body) {
  if (!body.name || !String(body.name).trim()) {
    return { ok: false, message: 'Lütfen ürün adı girin.' };
  }
  const category = parseInt(body.category, 10);
  if (![MenuCategory.Yemek, MenuCategory.Icecek, MenuCategory.Tatli].includes(category)) {
    return { ok: false, message: 'Lütfen geçerli bir kategori seçin.' };
  }
  const price = parseFloat(body.price);
  if (!Number.isFinite(price) || price < 0) {
    return { ok: false, message: 'Lütfen geçerli bir fiyat girin.' };
  }
  return { ok: true, category, price };
}

server.listen(PORT, () => {
  console.log(`RestaurantManager backend calisiyor: http://localhost:${PORT}`);
});
