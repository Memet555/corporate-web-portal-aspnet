using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Models;
using System.Text.Json;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace WebPortal.Controllers
{
    [AllowAnonymous]
    public class DynamicPageController : Controller
    {
        private readonly ILogger<DynamicPageController> _logger;
        private readonly WebPortalDbContext _context;

        public DynamicPageController(ILogger<DynamicPageController> logger, WebPortalDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Show(string url)
        {
            try
            {
                _logger.LogInformation("Show action called with URL: {Url}", url);

                // URL'den menüyü bul
                var menu = await _context.SidebarMenus
                    .FirstOrDefaultAsync(m => m.Url == $"/{url}" || m.Url == url);

                if (menu == null)
                {
                    _logger.LogWarning("Menu not found for URL: {Url}", url);
                    return NotFound();
                }

                _logger.LogInformation("Found menu: {Title}, PageType: {PageType}, URL: {Url}", 
                    menu.Title, menu.PageType, menu.Url);

                // Sayfa tipine göre view seç
                switch (menu.PageType?.ToLower())
                {
                    case "report":
                        return View("Report", menu);
                    
                    case "form":
                        return View("Form", menu);
                    
                    case "list":
                        return View("List", menu);
                    
                    case "dashboard":
                        return View("Dashboard", menu);
                    
                    default:
                        return View("Default", menu);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DynamicPage Show action'da hata oluştu");
                return StatusCode(500, "Bir hata oluştu");
            }
        }

        // Menü verilerini JSON olarak döndür (AJAX için)
        [HttpGet]
        public async Task<IActionResult> GetMenus()
        {
            try
            {
                var menus = await _context.SidebarMenus
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new
                    {
                        m.Id,
                        m.Title,
                        m.Url,
                        m.IconClass,
                        m.ParentId,
                        m.IsSubmenu,
                        m.PageType,
                        m.RequiredRole
                    })
                    .ToListAsync();

                return Json(menus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menüler getirilirken hata oluştu");
                return StatusCode(500, "Menüler yüklenirken hata oluştu");
            }
        }

        // Alt menüleri getir
        [HttpGet]
        public async Task<IActionResult> GetSubmenus(int parentId)
        {
            try
            {
                var submenus = await _context.SidebarMenus
                    .Where(m => m.ParentId == parentId && m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new
                    {
                        m.Id,
                        m.Title,
                        m.Url,
                        m.IconClass,
                        m.PageType,
                        m.RequiredRole
                    })
                    .ToListAsync();

                return Json(submenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alt menüler getirilirken hata oluştu");
                return StatusCode(500, "Alt menüler yüklenirken hata oluştu");
            }
        }

        // Rapor ekleme action'ı
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReport([FromForm] GenericReport model)
        {
            try
            {
                _logger.LogInformation("CreateReport action başladı");
                _logger.LogInformation($"Model Title: {model.Title}");
                _logger.LogInformation($"Model Description: {model.Description}");
                _logger.LogInformation($"Model ReportFile: {model.ReportFile?.FileName ?? "NULL"}");

                if (ModelState.IsValid)
                {
                    // Eğer Section formdan gelmemişse güvenli varsayılan ver
                    if (string.IsNullOrWhiteSpace(model.Section))
                    {
                        model.Section = "Genel";
                    }

                    // Dosya yükleme işlemi
                    if (model.ReportFile != null && model.ReportFile.Length > 0)
                    {
                        _logger.LogInformation($"Dosya yükleniyor: {model.ReportFile.FileName}, Boyut: {model.ReportFile.Length} bytes");
                        
                        // Dosya boyutu kontrolü (10MB)
                        if (model.ReportFile.Length > 10 * 1024 * 1024)
                        {
                            _logger.LogWarning("Dosya boyutu çok büyük: {Size} bytes", model.ReportFile.Length);
                            return Json(new { success = false, message = "Dosya boyutu 10MB'dan büyük olamaz." });
                        }

                        // Dosya uzantısı kontrolü
                        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip" };
                        var fileExtension = Path.GetExtension(model.ReportFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            _logger.LogWarning("Geçersiz dosya uzantısı: {Extension}", fileExtension);
                            return Json(new { success = false, message = "Sadece PDF, Word, Excel ve ZIP dosyaları kabul edilir." });
                        }

                        // Dosya adını güvenli hale getir
                        var fileName = Path.GetFileName(model.ReportFile.FileName);
                        var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{fileName}";
                        
                        _logger.LogInformation($"Dosya kaydediliyor: {uniqueFileName}");
                        
                        // Uploads klasörüne kaydet
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                            _logger.LogInformation("Uploads klasörü oluşturuldu: {Path}", uploadPath);
                        }
                        
                        var filePath = Path.Combine(uploadPath, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ReportFile.CopyToAsync(stream);
                        }

                        _logger.LogInformation("Dosya başarıyla kaydedildi: {Path}", filePath);

                        // Model'e dosya bilgilerini kaydet
                        model.FileName = uniqueFileName;
                        model.ReportName = fileName;
                        model.ReportDate = DateTime.Now;
                    }

                    _logger.LogInformation("Rapor bölümü: {Section}", model.Section);

                    model.IsActive = true;
                    model.CreatedAt = DateTime.Now;
                    model.CreatedBy = User.Identity?.Name ?? "Admin";
                    model.UpdatedAt = DateTime.Now;
                    model.UpdatedBy = User.Identity?.Name ?? "Admin";

                    _logger.LogInformation("Model veritabanına ekleniyor...");
                    _context.GenericReports.Add(model);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Rapor başarıyla veritabanına kaydedildi. ID: {Id}", model.Id);

                    return Json(new { 
                        success = true, 
                        message = "Rapor başarıyla eklendi!",
                        report = new {
                            id = model.Id,
                            title = model.Title,
                            type = model.ReportType,
                            description = model.Description,
                            fileName = model.FileName,
                            size = model.ReportFile?.Length ?? 0
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("ModelState geçersiz");
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Validation hatası", errors = errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor eklenirken hata oluştu: {Message}", ex.Message);
                return Json(new { success = false, message = $"Rapor eklenirken bir hata oluştu: {ex.Message}" });
            }
        }

        // Raporları getir
        [HttpGet]
        public async Task<IActionResult> GetReports(string? section)
        {
            try
            {
                var currentSection = string.IsNullOrWhiteSpace(section) ? "Genel" : section;
                _logger.LogInformation("GetReports çağrıldı. Bölüm: {Section}", currentSection);

                var reports = await _context.GenericReports
                    .Where(r => r.IsActive && r.Section == currentSection)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.ReportType,
                        r.Description,
                        r.FileName,
                        r.ReportDate,
                        r.IsActive,
                        r.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Bulunan rapor sayısı: {Count}", reports.Count);

                return Json(new { success = true, reports = reports });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raporlar getirilirken hata oluştu: {Message}", ex.Message);
                return Json(new { success = false, message = $"Raporlar getirilirken bir hata oluştu: {ex.Message}" });
            }
        }

        // Rapor sil
        [HttpPost]
        public async Task<IActionResult> DeleteReport(int id)
        {
            try
            {
                var report = await _context.GenericReports.FindAsync(id);
                if (report == null)
                {
                    return Json(new { success = false, message = "Rapor bulunamadı." });
                }

                // Dosyayı da sil
                if (!string.IsNullOrEmpty(report.FileName))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", report.FileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Dosya silindi: {Path}", filePath);
                    }
                }

                _context.GenericReports.Remove(report);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rapor başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor silinirken hata oluştu: {Message}", ex.Message);
                return Json(new { success = false, message = $"Rapor silinirken bir hata oluştu: {ex.Message}" });
            }
        }

        // Tek rapor getir
        [HttpGet]
        public async Task<IActionResult> GetReport(int id)
        {
            try
            {
                var report = await _context.GenericReports
                    .Where(r => r.Id == id && r.IsActive)
                    .Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.ReportType,
                        r.Description,
                        r.FileName,
                        r.ReportDate,
                        r.IsActive,
                        r.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (report == null)
                {
                    return Json(new { success = false, message = "Rapor bulunamadı." });
                }

                return Json(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor detayı getirilirken hata oluştu: {Message}", ex.Message);
                return Json(new { success = false, message = $"Rapor detayı getirilirken bir hata oluştu: {ex.Message}" });
            }
        }

        // Rapor dosyasını tarayıcıda görüntüle
        [HttpGet]
        public async Task<IActionResult> ViewReportFile(int id)
        {
            try
            {
                var report = await _context.GenericReports.FindAsync(id);
                if (report == null || string.IsNullOrEmpty(report.FileName))
                {
                    return NotFound("Dosya bulunamadı");
                }

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                var filePath = Path.Combine(uploadPath, report.FileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Dosya mevcut değil");
                }

                var contentType = report.FileName.ToLowerInvariant().EndsWith(".pdf") ? "application/pdf"
                    : report.FileName.ToLowerInvariant().EndsWith(".doc") || report.FileName.ToLowerInvariant().EndsWith(".docx") ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    : report.FileName.ToLowerInvariant().EndsWith(".xls") || report.FileName.ToLowerInvariant().EndsWith(".xlsx") ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : report.FileName.ToLowerInvariant().EndsWith(".zip") ? "application/zip"
                    : "application/octet-stream";

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{report.ReportName}\"";
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya görüntülenirken hata oluştu");
                return StatusCode(500, "Dosya görüntülenirken bir hata oluştu");
            }
        }
    }
}
