using Microsoft.EntityFrameworkCore;
using WebPortal.Models;

namespace WebPortal.Data
{
    public class WebPortalDbContext : DbContext
    {
        public WebPortalDbContext(DbContextOptions<WebPortalDbContext> options) : base(options) { }

        // Ana tablolar
        public DbSet<User> Users { get; set; }
        public DbSet<Duyuru> Duyurular { get; set; }
        public DbSet<SidebarMenu> SidebarMenus { get; set; }
        public DbSet<PortalBolum> PortalBolumlar { get; set; }
        public DbSet<PortalBolumDosya> PortalBolumDosyalar { get; set; }
        public DbSet<FoodMenu> FoodMenus { get; set; }
        public DbSet<BreakfastMenu> BreakfastMenus { get; set; }
        public DbSet<ExcelMenuFile> ExcelMenuFiles { get; set; }
        public DbSet<GenericReport> GenericReports { get; set; }
        public DbSet<MissionVision> MissionVision { get; set; }
        public DbSet<YonetimMesaji> YonetimMesajlari { get; set; }
        public DbSet<YonetimMesajiDosya> YonetimMesajiDosyalar { get; set; }
        public DbSet<AnnouncementFile> AnnouncementFiles { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User tablosu konfigürasyonu
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Duyuru tablosu konfigürasyonu
            modelBuilder.Entity<Duyuru>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Baslik).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Aciklama).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Tarih).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.CreatedBy).IsRequired();
                entity.Property(e => e.UpdatedAt);
                entity.Property(e => e.UpdatedBy);
                entity.Property(e => e.MiniFotoIndex);
                
                // Ignore unused columns that exist in database but not in model
                entity.Ignore(e => e.Icerik);
                entity.Ignore(e => e.Ikon);
                entity.Ignore(e => e.Kategori);
                
                // Files navigation property kullanılıyor
            });

            // SidebarMenu tablosu konfigürasyonu
            modelBuilder.Entity<SidebarMenu>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Url).HasMaxLength(200);
                entity.Property(e => e.IconClass).HasMaxLength(100);
                entity.Property(e => e.RequiredRole).HasMaxLength(20);
                entity.Property(e => e.PageType).HasMaxLength(20);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);

                // Self-referencing relationship
                entity.HasOne(e => e.Parent)
                      .WithMany(e => e.Submenus)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.DisplayOrder);
            });

            // PortalBolum tablosu konfigürasyonu
            modelBuilder.Entity<PortalBolum>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Icon).HasMaxLength(100);
            });

            // PortalBolumDosya tablosu konfigürasyonu
            modelBuilder.Entity<PortalBolumDosya>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SectionName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).IsRequired(false).HasColumnType("nvarchar(max)");
                entity.Property(e => e.UploadDate).IsRequired();

                // File property'si veritabanına gitmeyecek
                entity.Ignore(e => e.File);
            });

            // FoodMenu tablosu konfigürasyonu
            modelBuilder.Entity<FoodMenu>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Tarih).IsRequired();
                entity.Property(e => e.MenuIcerigi).HasColumnType("nvarchar(max)");
                entity.Property(e => e.DosyaAdi).HasMaxLength(255);
                entity.Property(e => e.DosyaYolu).HasMaxLength(500);
                entity.Property(e => e.YukleyenKullanici).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ExcelDosyaHash).HasMaxLength(100);
            });

            // BreakfastMenu tablosu konfigürasyonu
            modelBuilder.Entity<BreakfastMenu>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Tarih).IsRequired();
                entity.Property(e => e.MenuIcerigi).HasColumnType("nvarchar(max)");
                entity.Property(e => e.DosyaAdi).HasMaxLength(255);
                entity.Property(e => e.DosyaYolu).HasMaxLength(500);
                entity.Property(e => e.YukleyenKullanici).HasMaxLength(100);
                entity.Property(e => e.ExcelDosyaHash).HasMaxLength(100);
            });

            // ExcelMenuFile tablosu konfigürasyonu
            modelBuilder.Entity<ExcelMenuFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DosyaAdi).IsRequired().HasMaxLength(255);
                entity.Property(e => e.DosyaYolu).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DosyaHash).IsRequired().HasMaxLength(100);
                entity.Property(e => e.YukleyenKullanici).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Tip).IsRequired().HasMaxLength(20);
            });

            // GenericReport tablosu konfigürasyonu
            modelBuilder.Entity<GenericReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReportName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Section).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.ReportDate).IsRequired();

                // ReportFile property'si veritabanına gitmeyecek
                entity.Ignore(e => e.ReportFile);
            });

            // MissionVision tablosu konfigürasyonu
            modelBuilder.Entity<MissionVision>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Misyon).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.Vizyon).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.GuncellemeTarihi).IsRequired();
                entity.Property(e => e.GuncelleyenKullanici).IsRequired().HasMaxLength(100);
            });

            // YonetimMesaji tablosu konfigürasyonu
            modelBuilder.Entity<YonetimMesaji>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Mesaj).HasColumnType("nvarchar(max)");
                entity.Property(e => e.GecerlilikTarihi).IsRequired();
                entity.Property(e => e.DosyaAdi).HasMaxLength(255);

                // Dosya property'si veritabanına gitmeyecek
                entity.Ignore(e => e.Dosya);
            });

            // YonetimMesajiDosya tablosu konfigürasyonu
            modelBuilder.Entity<YonetimMesajiDosya>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DosyaAdi).IsRequired().HasMaxLength(255);
                entity.Property(e => e.DosyaYolu).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileType).HasMaxLength(20);
                entity.Property(e => e.YukleyenKullanici).IsRequired().HasMaxLength(100);
                entity.Property(e => e.GuncelleyenKullanici).HasMaxLength(100);
                entity.Property(e => e.Aciklama).HasMaxLength(1000);

                // File property'si veritabanına gitmeyecek
                entity.Ignore(e => e.File);

                // YonetimMesaji ile ilişki
                entity.HasOne(e => e.YonetimMesaji)
                      .WithMany(e => e.Dosyalar)
                      .HasForeignKey(e => e.YonetimMesajiId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AnnouncementFile tablosu konfigürasyonu (Duyuru dosyaları için)
            modelBuilder.Entity<AnnouncementFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileSize);
                entity.Property(e => e.UploadedAt).IsRequired();
                entity.Property(e => e.UploadedBy).IsRequired();
                
                // DuyuruId property'sini veritabanındaki DuyuruId kolonuna map'le
                entity.Property(e => e.DuyuruId).HasColumnName("DuyuruId");

                // Duyuru ile ilişki
                entity.HasOne(e => e.Duyuru)
                      .WithMany(e => e.Files)
                      .HasForeignKey(e => e.DuyuruId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
