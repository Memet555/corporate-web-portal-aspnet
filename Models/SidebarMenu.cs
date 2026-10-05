using System.ComponentModel.DataAnnotations;

namespace WebPortal.Models
{
    public class SidebarMenu
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Menü başlığı zorunludur")]
        [StringLength(50, ErrorMessage = "Başlık en fazla 50 karakter olabilir")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "URL en fazla 100 karakter olabilir")]
        public string? Url { get; set; }
        
        [StringLength(50, ErrorMessage = "İkon sınıfı en fazla 50 karakter olabilir")]
        public string? IconClass { get; set; }
        
        [StringLength(20, ErrorMessage = "Rol en fazla 20 karakter olabilir")]
        public string? RequiredRole { get; set; }
        
        public int? ParentId { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsSubmenu { get; set; } = false;
        
        [StringLength(20, ErrorMessage = "Sayfa türü en fazla 20 karakter olabilir")]
        public string? PageType { get; set; } = "Report"; // Report, Custom, External
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string? UpdatedBy { get; set; }
        
        // Navigation properties
        [System.Text.Json.Serialization.JsonIgnore]
        public SidebarMenu? Parent { get; set; }
        
        public List<SidebarMenu> Submenus { get; set; } = new List<SidebarMenu>();
    }
}
