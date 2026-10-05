using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Models;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GenericReportController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<GenericReportController> _logger;

        public GenericReportController(WebPortalDbContext context, ILogger<GenericReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var reports = await _context.GenericReports
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

        public IActionResult Create()
        {
            return View(new GenericReport());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GenericReport model)
        {
            _logger.LogInformation("Create action başladı");
            _logger.LogInformation($"Model Title: {model.Title}");
            _logger.LogInformation($"Model Description: {model.Description}");
            _logger.LogInformation($"Model ReportFile: {model.ReportFile?.FileName ?? "NULL"}");
            _logger.LogInformation($"ModelState IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState geçersiz:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation Error: {Error}", error.ErrorMessage);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("ModelState geçerli, işlem başlıyor...");
                    
                    // Dosya yükleme işlemi
                    if (model.ReportFile != null && model.ReportFile.Length > 0)
                    {
                        _logger.LogInformation($"Dosya yükleniyor: {model.ReportFile.FileName}, Boyut: {model.ReportFile.Length} bytes");
                        
                        // Dosya boyutu kontrolü (10MB)
                        if (model.ReportFile.Length > 10 * 1024 * 1024)
                        {
                            _logger.LogWarning("Dosya boyutu çok büyük: {Size} bytes", model.ReportFile.Length);
                            ModelState.AddModelError("ReportFile", "Dosya boyutu 10MB'dan büyük olamaz.");
                            return View(model);
                        }

                        // Dosya uzantısı kontrolü
                        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip" };
                        var fileExtension = Path.GetExtension(model.ReportFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            _logger.LogWarning("Geçersiz dosya uzantısı: {Extension}", fileExtension);
                            ModelState.AddModelError("ReportFile", "Sadece PDF, Word, Excel ve ZIP dosyaları kabul edilir.");
                            return View(model);
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
                    else
                    {
                        _logger.LogWarning("Dosya yüklenmedi veya boş");
                    }

                    model.IsActive = true;
                    model.CreatedAt = DateTime.Now;
                    model.CreatedBy = User.Identity?.Name ?? "Admin";
                    model.UpdatedAt = DateTime.Now;
                    model.UpdatedBy = User.Identity?.Name ?? "Admin";

                    _logger.LogInformation("Model veritabanına ekleniyor...");
                    _context.GenericReports.Add(model);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Rapor başarıyla veritabanına kaydedildi. ID: {Id}", model.Id);

                    TempData["Success"] = "Rapor başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rapor eklenirken hata oluştu: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Rapor eklenirken bir hata oluştu: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("ModelState geçersiz, form tekrar gösteriliyor");
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var report = await _context.GenericReports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GenericReport model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var report = await _context.GenericReports.FirstOrDefaultAsync(r => r.Id == id);
                    if (report == null)
                    {
                        return NotFound();
                    }

                    report.Title = model.Title;
                    report.Content = model.Content;
                    report.ReportType = model.ReportType;
                    report.IsActive = model.IsActive;
                    report.UpdatedAt = DateTime.Now;
                    report.UpdatedBy = User.Identity?.Name ?? "Admin";

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Rapor başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rapor güncellenirken hata oluştu");
                    ModelState.AddModelError("", "Rapor güncellenirken bir hata oluştu.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var report = await _context.GenericReports.FirstOrDefaultAsync(r => r.Id == id);
                if (report == null)
                {
                    return Json(new { success = false, message = "Rapor bulunamadı." });
                }

                _context.GenericReports.Remove(report);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rapor başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor silinirken hata oluştu");
                return Json(new { success = false, message = "Rapor silinirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var report = await _context.GenericReports.FirstOrDefaultAsync(r => r.Id == id);
                if (report == null)
                {
                    return Json(new { success = false, message = "Rapor bulunamadı." });
                }

                report.IsActive = !report.IsActive;
                report.UpdatedAt = DateTime.Now;
                report.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Rapor {(report.IsActive ? "aktif" : "pasif")} yapıldı.", isActive = report.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor durumu değiştirilirken hata oluştu");
                return Json(new { success = false, message = "Rapor durumu değiştirilirken bir hata oluştu." });
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
    }
}
