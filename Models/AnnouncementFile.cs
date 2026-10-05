namespace WebPortal.Models
{
    public class AnnouncementFile
    {
        public int Id { get; set; }
        public int DuyuruId { get; set; } // Model'de DuyuruId kullan, veritabanında AnnouncementId'ye map'le
        public string FileType { get; set; } = string.Empty; // "Photo", "Document", "MiniPhoto"
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = string.Empty;

        // Navigation property
        [System.Text.Json.Serialization.JsonIgnore]
        public Duyuru Duyuru { get; set; } = null!;
    }
}
