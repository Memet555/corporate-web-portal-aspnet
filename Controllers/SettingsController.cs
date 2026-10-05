using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Models;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(WebPortalDbContext context, ILogger<SettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var settings = await _context.Settings
                    .OrderBy(s => s.Key)
                    .ToListAsync();

                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayarlar yüklenirken hata oluştu");
                return View(new List<Setting>());
            }
        }

        public IActionResult Create()
        {
            return View(new Setting());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Setting model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Aynı key'e sahip ayar var mı kontrol et
                    var existingSetting = await _context.Settings
                        .FirstOrDefaultAsync(s => s.Key == model.Key);

                    if (existingSetting != null)
                    {
                        ModelState.AddModelError("Key", "Bu anahtar zaten kullanılıyor.");
                        return View(model);
                    }

                    model.IsActive = true;
                    model.CreatedAt = DateTime.Now;
                    model.CreatedBy = User.Identity?.Name ?? "Admin";
                    model.UpdatedAt = DateTime.Now;
                    model.UpdatedBy = User.Identity?.Name ?? "Admin";

                    _context.Settings.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Ayar başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ayar eklenirken hata oluştu");
                    ModelState.AddModelError("", "Ayar eklenirken bir hata oluştu.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Id == id);
            if (setting == null)
            {
                return NotFound();
            }

            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Setting model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Id == id);
                    if (setting == null)
                    {
                        return NotFound();
                    }

                    // Key değiştirilmişse duplicate kontrolü yap
                    if (setting.Key != model.Key)
                    {
                        var existingSetting = await _context.Settings
                            .FirstOrDefaultAsync(s => s.Key == model.Key && s.Id != id);

                        if (existingSetting != null)
                        {
                            ModelState.AddModelError("Key", "Bu anahtar zaten kullanılıyor.");
                            return View(model);
                        }
                    }

                    setting.Key = model.Key;
                    setting.Value = model.Value;
                    setting.Description = model.Description;
                    setting.IsActive = model.IsActive;
                    setting.UpdatedAt = DateTime.Now;
                    setting.UpdatedBy = User.Identity?.Name ?? "Admin";

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Ayar başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ayar güncellenirken hata oluştu");
                    ModelState.AddModelError("", "Ayar güncellenirken bir hata oluştu.");
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
                var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Id == id);
                if (setting == null)
                {
                    return Json(new { success = false, message = "Ayar bulunamadı." });
                }

                _context.Settings.Remove(setting);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ayar başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayar silinirken hata oluştu");
                return Json(new { success = false, message = "Ayar silinirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Id == id);
                if (setting == null)
                {
                    return Json(new { success = false, message = "Ayar bulunamadı." });
                }

                setting.IsActive = !setting.IsActive;
                setting.UpdatedAt = DateTime.Now;
                setting.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Ayar {(setting.IsActive ? "aktif" : "pasif")} yapıldı.", isActive = setting.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayar durumu değiştirilirken hata oluştu");
                return Json(new { success = false, message = "Ayar durumu değiştirilirken bir hata oluştu." });
            }
        }

        // AJAX ile hızlı güncelleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdate(int id, string value)
        {
            try
            {
                var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Id == id);
                if (setting == null)
                {
                    return Json(new { success = false, message = "Ayar bulunamadı." });
                }

                setting.Value = value;
                setting.UpdatedAt = DateTime.Now;
                setting.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ayar güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayar hızlı güncelleme sırasında hata oluştu");
                return Json(new { success = false, message = "Ayar güncellenirken bir hata oluştu." });
            }
        }

        // Ayar değerini getir (AJAX için)
        [HttpGet]
        public async Task<IActionResult> GetValue(string key)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Key == key && s.IsActive);

                if (setting == null)
                {
                    return Json(new { success = false, message = "Ayar bulunamadı." });
                }

                return Json(new { success = true, value = setting.Value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayar değeri getirilirken hata oluştu");
                return Json(new { success = false, message = "Ayar değeri getirilirken bir hata oluştu." });
            }
        }
    }
}
