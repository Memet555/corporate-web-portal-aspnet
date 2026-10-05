using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace WebPortal.Models;

public class YonetimMesaji
{
    public int Id { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Mesaj { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime GecerlilikTarihi { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    
    [JsonIgnore]
    public IFormFile? Dosya { get; set; }

    // Navigation property
    [JsonIgnore]
    public List<YonetimMesajiDosya> Dosyalar { get; set; } = new List<YonetimMesajiDosya>();
}
