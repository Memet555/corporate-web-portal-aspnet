using WebPortal.Models;
using System.Security.Cryptography;
using System.Text;

namespace WebPortal.Data
{
    public static class DbInitializer
    {
        public static void Initialize(WebPortalDbContext context)
        {
            // Veritabanının oluşturulduğundan emin ol
            context.Database.EnsureCreated();

            // Kullanıcılar zaten varsa seed yapma
            if (context.Users.Any())
                return;

            // Varsayılan kullanıcıları ekle
            var users = new List<User>
            {
                new User { Username = "admin", Password = HashPassword("admin123"), Role = "Admin" },
                new User { Username = "user", Password = HashPassword("user123"), Role = "User" }
            };

            context.Users.AddRange(users);

            // Varsayılan ayarları ekle
            var settings = new List<Setting>
            {
                new Setting { Key = "SiteTitle", Value = "Web Portal", Description = "Site başlığı", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "System" },
                new Setting { Key = "SiteDescription", Value = "Kurumsal Web Portal", Description = "Site açıklaması", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "System" },
                new Setting { Key = "MaxFileSize", Value = "52428800", Description = "Maksimum dosya boyutu (byte)", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "System" },
                new Setting { Key = "AllowedFileTypes", Value = "pdf,doc,docx,xls,xlsx,jpg,jpeg,png,gif,zip,rar", Description = "İzin verilen dosya türleri", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "System" }
            };

            context.Settings.AddRange(settings);

            // Varsayılan sidebar menüleri ekle
            var sidebarMenus = new List<SidebarMenu>
            {
                new SidebarMenu
                {
                    Title = "Ana Sayfa",
                    Url = "/",
                    IconClass = "fa-solid fa-house",
                    DisplayOrder = 1,
                    IsActive = true,
                    IsSubmenu = false,
                    PageType = "Custom",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new SidebarMenu
                {
                    Title = "Yönetim Mesajı",
                    Url = "/Home/YonetimMesaji",
                    IconClass = "fa-solid fa-envelope",
                    DisplayOrder = 2,
                    IsActive = true,
                    IsSubmenu = false,
                    PageType = "Custom",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new SidebarMenu
                {
                    Title = "Raporlar",
                    IconClass = "fa-solid fa-chart-line",
                    DisplayOrder = 3,
                    IsActive = true,
                    IsSubmenu = false,
                    PageType = "Report",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new SidebarMenu
                {
                    Title = "Satış Raporları",
                    Url = "/Reports/Sales",
                    IconClass = "fa-solid fa-chart-bar",
                    ParentId = 3,
                    DisplayOrder = 1,
                    IsActive = true,
                    IsSubmenu = true,
                    PageType = "Report",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new SidebarMenu
                {
                    Title = "Stok Raporları",
                    Url = "/Reports/Stock",
                    IconClass = "fa-solid fa-boxes-stacked",
                    ParentId = 3,
                    DisplayOrder = 2,
                    IsActive = true,
                    IsSubmenu = true,
                    PageType = "Report",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                }
            };

            context.SidebarMenus.AddRange(sidebarMenus);

            // Varsayılan portal bölümleri ekle
            var portalBolumlar = new List<PortalBolum>
            {
                new PortalBolum { Baslik = "Bossa", Aciklama = "Bossa ile ilgili içerikler.", IconClass = "fa-solid fa-building" },
                new PortalBolum { Baslik = "IT", Aciklama = "IT departmanı içerikleri.", IconClass = "fa-solid fa-laptop-code" },
                new PortalBolum { Baslik = "Kartvizit", Aciklama = "Kartvizit yönetimi.", IconClass = "fa-solid fa-address-card" },
                new PortalBolum { Baslik = "Telefon", Aciklama = "Telefon rehberi.", IconClass = "fa-solid fa-phone" }
            };

            context.PortalBolumlar.AddRange(portalBolumlar);

            // Varsayılan misyon vizyon ekle
            var missionVision = new MissionVision
            {
                Baslik = "Misyon",
                Icerik = "Müşterilerimize en yenilikçi ve kaliteli çözümleri sunarak sektörde öncü olmak.",
                Tip = "Mission",
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = "System"
            };

            context.MissionVision.Add(missionVision);

            // Örnek duyuru ekle
            var duyuru = new Duyuru
            {
                Baslik = "Hoş Geldiniz!",
                Aciklama = "Web Portal sistemine hoş geldiniz. Bu sistem ile tüm işlemlerinizi kolayca yönetebilirsiniz.",
                Tarih = DateTime.Now,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = "System"
            };

            context.Duyurular.Add(duyuru);

            // Değişiklikleri kaydet
            context.SaveChanges();
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
