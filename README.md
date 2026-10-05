# WebPortal - Kurumsal Web Portal Uygulaması

## 📋 Proje Hakkında

WebPortal, kurumsal ihtiyaçlar için geliştirilmiş kapsamlı bir web portal uygulamasıdır. Bu uygulama, duyuru yönetimi, rapor sistemi, menü yönetimi, portal bölümleri ve kullanıcı yönetimi gibi temel kurumsal işlevleri sağlar.

## ✨ Özellikler

### 🔐 Kimlik Doğrulama ve Yetkilendirme
- Cookie tabanlı kimlik doğrulama sistemi
- Rol tabanlı yetkilendirme (Admin, User vb.)
- Güvenli giriş/çıkış işlemleri

### 📢 Duyuru Yönetimi
- Duyuru oluşturma, düzenleme ve silme
- Duyuru kategorileri ve etiketler
- Duyuru dosya ekleme desteği
- Aktif/pasif duyuru durumu

### 📊 Rapor Sistemi
- Generic rapor oluşturma ve yönetimi
- Excel dosya desteği (ClosedXML)
- Dinamik rapor görüntüleme
- Rapor kategorileri ve bölümleri

### ��️ Portal Bölümleri
- Dinamik portal bölümleri oluşturma
- Dosya yükleme ve yönetimi
- Bölüm bazlı içerik organizasyonu
- Hiyerarşik menü yapısı

### �� Gelişmiş Arama
- Çoklu kategori arama (menüler, raporlar, duyurular, portal bölümleri)
- Otomatik tamamlama önerileri
- Arama sonuçlarında filtreleme

### ⚙️ Sistem Yönetimi
- Kullanıcı yönetimi
- Rol ve izin yönetimi
- Sistem ayarları
- Yedekleme ve geri yükleme

### ��️ Menü Yönetimi
- Yemek menüleri (kahvaltı, öğle, akşam)
- Excel tabanlı menü yükleme
- Menü tarih yönetimi

## 🛠️ Teknolojiler

- **.NET 8.0** - Ana framework
- **ASP.NET Core MVC** - Web framework
- **Entity Framework Core 8.0** - ORM
- **SQL Server** - Veritabanı
- **Bootstrap** - UI framework
- **ClosedXML** - Excel işlemleri
- **jQuery** - JavaScript kütüphanesi

## 📦 Kurulum

### Gereksinimler
- .NET 8.0 SDK
- SQL Server (Express veya üzeri)
- Visual Studio 2022 veya VS Code

### Adımlar

1. **Projeyi klonlayın**
   ```bash
   git clone [repository-url]
   cd WebPortal
   ```

2. **Veritabanı bağlantısını yapılandırın**
   - `appsettings.json` dosyasındaki `ConnectionStrings` bölümünü düzenleyin
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=WebPortalDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
   }
   ```

3. **Veritabanı migration'larını çalıştırın**
   ```bash
   dotnet ef database update
   ```

4. **Uygulamayı çalıştırın**
   ```bash
   dotnet run
   ```

5. **Tarayıcıda açın**
   ```
   https://localhost:5001
   ```

## 📄 Veritabanı Yapısı

### Ana Tablolar
- **Users** - Kullanıcı bilgileri
- **Duyurular** - Duyuru içerikleri
- **SidebarMenus** - Menü yapısı
- **PortalBolumlar** - Portal bölümleri
- **GenericReports** - Raporlar
- **FoodMenus** - Yemek menüleri
- **BreakfastMenus** - Kahvaltı menüleri
- **Settings** - Sistem ayarları

## 🔧 Yapılandırma

### Dosya Yükleme Limitleri
- Maksimum dosya boyutu: 50MB
- Desteklenen formatlar: Excel, PDF, Resim dosyaları

### Güvenlik Ayarları
- HTTPS zorunluluğu
- Cookie tabanlı kimlik doğrulama
- CSRF koruması

## 📄 Proje Yapısı

```
WebPortal/
├── Controllers/          # MVC Controller'ları
├── Models/              # Veri modelleri
├── Views/               # Razor view'ları
├── Data/                # DbContext ve migration'lar
├── Services/            # İş mantığı servisleri
├── Middleware/          # Özel middleware'ler
├── wwwroot/            # Statik dosyalar
└── Migrations/         # EF Core migration'ları
```

## 🚀 Kullanım

### Yönetici Paneli
- `/Admin` - Kullanıcı ve rol yönetimi
- `/Settings` - Sistem ayarları
- `/SidebarMenu` - Menü yönetimi

### İçerik Yönetimi
- `/Duyuru` - Duyuru yönetimi
- `/PortalBolumAdmin` - Portal bölüm yönetimi
- `/GenericReport` - Rapor yönetimi

### Kullanıcı Arayüzü
- `/Home` - Ana sayfa
- `/Search` - Arama fonksiyonu
- `/Reports` - Rapor görüntüleme

## 🔍 Arama Özellikleri

Uygulama gelişmiş arama özellikleri sunar:
- **Menü Arama**: Sidebar menülerinde arama
- **Rapor Arama**: Generic raporlarda arama
- **Duyuru Arama**: Duyuru başlık ve açıklamalarında arama
- **Portal Bölüm Arama**: Portal bölümlerinde arama

## 📄 Geliştirme Notları

### Migration Oluşturma
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Seed Data
Uygulama ilk çalıştırıldığında otomatik olarak seed data eklenir.

### Logging
Uygulama kapsamlı logging sistemi kullanır. Loglar `appsettings.json`'da yapılandırılabilir.


