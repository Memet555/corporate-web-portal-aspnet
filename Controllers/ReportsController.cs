using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Models;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(WebPortalDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var reports = await _context.GenericReports
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raporlar listelenirken hata oluştu");
                return View(new List<GenericReport>());
            }
        }

        // Rapor detayını görüntüleme (herkese açık)
        [AllowAnonymous]
        public async Task<IActionResult> View(int id)
        {
            try
            {
                var report = await _context.GenericReports.FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
                if (report == null)
                {
                    return NotFound("Rapor bulunamadı veya aktif değil.");
                }

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor görüntülenirken hata oluştu");
                return StatusCode(500, "Rapor görüntülenirken bir hata oluştu.");
            }
        }

        // Rapor arama
        [HttpGet]
        public async Task<IActionResult> Search(string query, string type)
        {
            try
            {
                var reportsQuery = _context.GenericReports.Where(r => r.IsActive);

                if (!string.IsNullOrEmpty(query))
                {
                    reportsQuery = reportsQuery.Where(r => 
                        r.Title.Contains(query) || 
                        r.Content.Contains(query) || 
                        r.Content.Contains(query));
                }

                if (!string.IsNullOrEmpty(type))
                {
                    reportsQuery = reportsQuery.Where(r => r.ReportType == type);
                }

                var reports = await reportsQuery
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return View("Index", reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor arama sırasında hata oluştu");
                return View("Index", new List<GenericReport>());
            }
        }

        // Rapor kategorileri
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            try
            {
                var categories = await _context.GenericReports
                    .Where(r => r.IsActive)
                    .GroupBy(r => r.ReportType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor kategorileri listelenirken hata oluştu");
                return View(new List<object>());
            }
        }

        // Satış raporları
        [AllowAnonymous]
        public async Task<IActionResult> Sales()
        {
            try
            {
                var reports = await _context.GenericReports
                    .Where(r => r.IsActive && r.ReportType == "Sales")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış raporları listelenirken hata oluştu");
                return View(new List<GenericReport>());
            }
        }

        // Stok raporları
        [AllowAnonymous]
        public async Task<IActionResult> Stock()
        {
            try
            {
                var reports = await _context.GenericReports
                    .Where(r => r.IsActive && r.ReportType == "Stock")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok raporları listelenirken hata oluştu");
                return View(new List<GenericReport>());
            }
        }

        // Rapor yükleme
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload([FromForm] GenericReport model, IFormFile reportFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    return Json(new { success = false, message = "Rapor başlığı zorunludur." });
                }

                if (reportFile != null && reportFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + "_" + reportFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await reportFile.CopyToAsync(stream);
                    }

                    model.FileName = fileName;
                }

                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                model.CreatedBy = User.Identity?.Name ?? "Admin";
                model.UpdatedAt = DateTime.Now;
                model.UpdatedBy = User.Identity?.Name ?? "Admin";
                model.ReportDate = DateTime.Now;

                _context.GenericReports.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rapor başarıyla yüklendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor yüklenirken hata oluştu");
                return Json(new { success = false, message = "Rapor yüklenirken bir hata oluştu." });
            }
        }

        // Rapor düzenleme
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] GenericReport model, IFormFile reportFile)
        {
            try
            {
                var existingReport = await _context.GenericReports.FindAsync(model.Id);
                if (existingReport == null)
                {
                    return Json(new { success = false, message = "Rapor bulunamadı." });
                }

                if (reportFile != null && reportFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + "_" + reportFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await reportFile.CopyToAsync(stream);
                    }

                    // Eski dosyayı sil
                    if (!string.IsNullOrEmpty(existingReport.FileName))
                    {
                        var oldFilePath = Path.Combine(uploadsFolder, existingReport.FileName);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    existingReport.FileName = fileName;
                }

                existingReport.Title = model.Title;
                existingReport.Description = model.Description;
                existingReport.ReportType = model.ReportType;
                existingReport.UpdatedAt = DateTime.Now;
                existingReport.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rapor başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor güncellenirken hata oluştu");
                return Json(new { success = false, message = "Rapor güncellenirken bir hata oluştu." });
            }
        }

        // Rapor silme
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var report = await _context.GenericReports.FindAsync(id);
                if (report == null)
                {
                    return Json(new { success = false, message = "Rapor bulunamadı." });
                }

                report.IsActive = false;
                report.UpdatedAt = DateTime.Now;
                report.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rapor başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor silinirken hata oluştu");
                return Json(new { success = false, message = "Rapor silinirken bir hata oluştu." });
            }
        }

        // Rapor istatistikleri
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var stats = new
                {
                    TotalReports = await _context.GenericReports.CountAsync(),
                    ActiveReports = await _context.GenericReports.CountAsync(r => r.IsActive),
                    ReportsByType = await _context.GenericReports
                        .Where(r => r.IsActive && !string.IsNullOrEmpty(r.ReportType))
                        .GroupBy(r => r.ReportType)
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    RecentReports = await _context.GenericReports
                        .Where(r => r.IsActive)
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(10)
                        .Select(r => new { r.Title, r.CreatedAt, r.ReportType })
                        .ToListAsync()
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor istatistikleri getirilirken hata oluştu");
                return Json(new { error = "İstatistikler yüklenirken hata oluştu." });
            }
        }

        // Excel export
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            try
            {
                var reports = await _context.GenericReports
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Title,
                        r.Content,
                        r.ReportType,
                        r.CreatedAt,
                        r.CreatedBy,
                        r.IsActive
                    })
                    .ToListAsync();

                // Excel export işlemi burada yapılacak
                // Şimdilik JSON olarak döndürüyoruz
                return Json(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor export sırasında hata oluştu");
                return Json(new { error = "Export sırasında hata oluştu." });
            }
        }
    }
}
