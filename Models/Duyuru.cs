
namespace WebPortal.Models;

public class Duyuru
{
    public int Id { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;

    public DateTime Tarih { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Mini fotoğraf seçimi için
    public int? MiniFotoIndex { get; set; } = 0;
    
    // Navigation property
    public List<AnnouncementFile> Files { get; set; } = new();
    
    // These properties are ignored in DbContext but exist in database
    // They are kept for backward compatibility but not used in the application
    public string? Icerik { get; set; }
    public string? Ikon { get; set; }
    public string? Kategori { get; set; }
}
