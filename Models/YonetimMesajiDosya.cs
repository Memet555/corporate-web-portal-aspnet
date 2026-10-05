using System;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace WebPortal.Models
{
    public class YonetimMesajiDosya
    {
        public int Id { get; set; }
        public int YonetimMesajiId { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public string DosyaYolu { get; set; } = string.Empty;
        public long? DosyaBoyutu { get; set; }
        public DateTime YuklenmeTarihi { get; set; }
        public string YukleyenKullanici { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public string? GuncelleyenKullanici { get; set; }
        public string FileType { get; set; } = string.Empty; // "Document", "Image", etc.
        
        [JsonIgnore]
        public IFormFile? File { get; set; }

        // Navigation property
        [JsonIgnore]
        public YonetimMesaji YonetimMesaji { get; set; } = null!;
    }
}
