using System;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace WebPortal.Models
{
    public class PortalBolumDosya
    {
        public int Id { get; set; }
        public int PortalBolumId { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public string DosyaYolu { get; set; } = string.Empty;
        public long? DosyaBoyutu { get; set; }
        public DateTime YuklenmeTarihi { get; set; }
        public string YukleyenKullanici { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public string? GuncelleyenKullanici { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UploadDate { get; set; }
        
        [JsonIgnore]
        public IFormFile? File { get; set; }

        // Navigation property
        public PortalBolum PortalBolum { get; set; } = null!;
    }
} 