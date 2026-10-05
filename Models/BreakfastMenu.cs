namespace WebPortal.Models
{
    public class BreakfastMenu
    {
        public int Id { get; set; }
        public string Zeytin { get; set; } = string.Empty;
        public string Peynir { get; set; } = string.Empty;
        public string Bal { get; set; } = string.Empty;
        public string Kaymak { get; set; } = string.Empty;
        public string Yumurta { get; set; } = string.Empty;
        public string Domates { get; set; } = string.Empty;
        public string Salatalik { get; set; } = string.Empty;
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
