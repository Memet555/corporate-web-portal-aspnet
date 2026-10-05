using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace WebPortal.Models
{
    public class GenericReport
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        
        public IFormFile? ReportFile { get; set; }
    }
}