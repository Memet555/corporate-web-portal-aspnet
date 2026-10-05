namespace WebPortal.Models
{
    public class FoodMenu
    {
        public int Id { get; set; }
        public string Corba { get; set; } = string.Empty;
        public string AnaYemek1 { get; set; } = string.Empty;
        public string AnaYemek2 { get; set; } = string.Empty;
        public string Pilav { get; set; } = string.Empty;
        public string Tatli { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string MenuIcerigi { get; set; } = string.Empty;
        public string DosyaAdi { get; set; } = string.Empty;
        public string DosyaYolu { get; set; } = string.Empty;
        public string YukleyenKullanici { get; set; } = string.Empty;
        public string ExcelDosyaHash { get; set; } = string.Empty;
    }
} 