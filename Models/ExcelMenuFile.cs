namespace WebPortal.Models
{
    public class ExcelMenuFile
    {
        public int Id { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public string DosyaYolu { get; set; } = string.Empty;
        public string DosyaHash { get; set; } = string.Empty;
        public DateTime YuklenmeTarihi { get; set; }
        public string YukleyenKullanici { get; set; } = string.Empty;
        public DateTime SonGuncelleme { get; set; }
        public bool Aktif { get; set; } = true;
        public string Tip { get; set; } = string.Empty; // "Yemek" veya "Kahvaltı"
    }
}
