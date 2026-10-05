namespace WebPortal.Models;

public class MissionVision
{
    public int Id { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty; // "Mission" veya "Vision"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string Misyon { get; set; } = string.Empty;
    public string Vizyon { get; set; } = string.Empty;
    public DateTime GuncellemeTarihi { get; set; }
    public string GuncelleyenKullanici { get; set; } = string.Empty;
} 