using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Models;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PortalBolumAdminController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<PortalBolumAdminController> _logger;

        public PortalBolumAdminController(WebPortalDbContext context, ILogger<PortalBolumAdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var bolumler = await _context.PortalBolumlar
                    .Include(b => b.Dosyalar)
                    .OrderBy(b => b.DisplayOrder)
                    .ToListAsync();

                return View(bolumler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portal bölümleri listelenirken hata oluştu");
                return View(new List<PortalBolum>());
            }
        }

        public IActionResult Create()
        {
            return View(new PortalBolum());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PortalBolum model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.IsActive = true;
                    model.CreatedAt = DateTime.Now;
                    model.CreatedBy = User.Identity?.Name ?? "Admin";
                    model.UpdatedAt = DateTime.Now;
                    model.UpdatedBy = User.Identity?.Name ?? "Admin";

                    // Display order belirle
                    var maxOrder = await _context.PortalBolumlar
                        .MaxAsync(b => (int?)b.DisplayOrder) ?? 0;
                    model.DisplayOrder = maxOrder + 1;

                    _context.PortalBolumlar.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Portal bölümü başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Portal bölümü eklenirken hata oluştu");
                    ModelState.AddModelError("", "Portal bölümü eklenirken bir hata oluştu.");
                }
            }

            return View(model);
        }

        // AJAX için portal bölümü getirme (düzenleme için)
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var bolum = await _context.PortalBolumlar
                    .Include(b => b.Dosyalar)
                    .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

                if (bolum == null)
                {
                    return Json(new { success = false, message = "Portal bölümü bulunamadı." });
                }

                var dosyalar = new List<object>();
                if (bolum.Dosyalar != null && bolum.Dosyalar.Any())
                {
                    dosyalar = bolum.Dosyalar.Select(d => new
                    {
                        id = d.Id,
                        fileName = d.DosyaAdi,
                        description = d.Aciklama,
                        uploadDate = d.YuklenmeTarihi
                    }).Cast<object>().ToList();
                }

                return Json(new
                {
                    success = true,
                    id = bolum.Id,
                    name = bolum.Baslik,
                    description = bolum.Aciklama,
                    files = dosyalar
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portal bölümü getirilirken hata oluştu");
                return Json(new { success = false, message = "Portal bölümü getirilirken bir hata oluştu." });
            }
        }

        // AJAX için portal bölümü düzenleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] PortalBolum model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Baslik))
                {
                    return Json(new { success = false, message = "Bölüm adı zorunludur." });
                }

                var bolum = await _context.PortalBolumlar
                    .FirstOrDefaultAsync(b => b.Id == model.Id && b.IsActive);

                if (bolum == null)
                {
                    return Json(new { success = false, message = "Portal bölümü bulunamadı." });
                }

                bolum.Baslik = model.Baslik;
                bolum.Aciklama = model.Aciklama;
                bolum.UpdatedAt = DateTime.Now;
                bolum.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Portal bölümü başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portal bölümü güncellenirken hata oluştu");
                return Json(new { success = false, message = "Portal bölümü güncellenirken bir hata oluştu." });
            }
        }

        // AJAX için portal bölümü ekleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] PortalBolum model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Baslik))
                {
                    return Json(new { success = false, message = "Bölüm adı zorunludur." });
                }

                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                model.CreatedBy = User.Identity?.Name ?? "Admin";
                model.UpdatedAt = DateTime.Now;
                model.UpdatedBy = User.Identity?.Name ?? "Admin";

                // Display order belirle
                var maxOrder = await _context.PortalBolumlar
                    .MaxAsync(b => (int?)b.DisplayOrder) ?? 0;
                model.DisplayOrder = maxOrder + 1;

                _context.PortalBolumlar.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Portal bölümü başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portal bölümü eklenirken hata oluştu");
                return Json(new { success = false, message = "Portal bölümü eklenirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var bolum = await _context.PortalBolumlar
                    .Include(b => b.Dosyalar)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bolum == null)
                {
                    return Json(new { success = false, message = "Portal bölümü bulunamadı." });
                }

                // Dosyaları da sil
                if (bolum.Dosyalar != null && bolum.Dosyalar.Any())
                {
                    _context.PortalBolumDosyalar.RemoveRange(bolum.Dosyalar);
                }

                _context.PortalBolumlar.Remove(bolum);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Portal bölümü başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portal bölümü silinirken hata oluştu");
                return Json(new { success = false, message = "Portal bölümü silinirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var bolum = await _context.PortalBolumlar.FirstOrDefaultAsync(b => b.Id == id);

                if (bolum == null)
                {
                    return Json(new { success = false, message = "Portal bölümü bulunamadı." });
                }

                bolum.IsActive = !bolum.IsActive;
                bolum.UpdatedAt = DateTime.Now;
                bolum.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Portal bölümü {(bolum.IsActive ? "aktif" : "pasif")} yapıldı.", isActive = bolum.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portal bölümü durumu değiştirilirken hata oluştu");
                return Json(new { success = false, message = "Portal bölümü durumu değiştirilirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder([FromBody] List<OrderUpdateModel> orderUpdates)
        {
            try
            {
                foreach (var update in orderUpdates)
                {
                    var bolum = await _context.PortalBolumlar.FirstOrDefaultAsync(b => b.Id == update.Id);
                    if (bolum != null)
                    {
                        bolum.DisplayOrder = update.Order;
                        bolum.UpdatedAt = DateTime.Now;
                        bolum.UpdatedBy = User.Identity?.Name ?? "Admin";
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Sıralama güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sıralama güncellenirken hata oluştu");
                return Json(new { success = false, message = "Sıralama güncellenirken bir hata oluştu." });
            }
        }

        // Dosya ekleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFile(int bolumId, IFormFile dosya)
        {
            try
            {
                if (dosya == null || dosya.Length == 0)
                {
                    return Json(new { success = false, message = "Dosya seçilmedi." });
                }

                var bolum = await _context.PortalBolumlar.FirstOrDefaultAsync(b => b.Id == bolumId);
                if (bolum == null)
                {
                    return Json(new { success = false, message = "Portal bölümü bulunamadı." });
                }

                var dosyaModel = new PortalBolumDosya
                {
                    PortalBolumId = bolumId,
                    DosyaAdi = dosya.FileName,
                    DosyaYolu = $"{Guid.NewGuid()}_{dosya.FileName}",
                    DosyaBoyutu = dosya.Length,
                    YuklenmeTarihi = DateTime.Now,
                    YukleyenKullanici = User.Identity?.Name ?? "Admin"
                };

                // Dosyayı fiziksel olarak kaydet
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "portal-bolumler");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, dosyaModel.DosyaYolu);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dosya.CopyToAsync(stream);
                }

                _context.PortalBolumDosyalar.Add(dosyaModel);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dosya başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya eklenirken hata oluştu");
                return Json(new { success = false, message = "Dosya eklenirken bir hata oluştu." });
            }
        }

        // Dosya silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(int dosyaId)
        {
            try
            {
                var dosya = await _context.PortalBolumDosyalar.FirstOrDefaultAsync(d => d.Id == dosyaId);
                if (dosya == null)
                {
                    return Json(new { success = false, message = "Dosya bulunamadı." });
                }

                // Fiziksel dosyayı sil
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "portal-bolumler", dosya.DosyaYolu);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.PortalBolumDosyalar.Remove(dosya);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dosya başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya silinirken hata oluştu");
                return Json(new { success = false, message = "Dosya silinirken bir hata oluştu." });
            }
        }

        // Dosya düzenleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFile(int dosyaId, string aciklama)
        {
            try
            {
                var dosya = await _context.PortalBolumDosyalar.FirstOrDefaultAsync(d => d.Id == dosyaId);
                if (dosya == null)
                {
                    return Json(new { success = false, message = "Dosya bulunamadı." });
                }

                dosya.Aciklama = aciklama;
                dosya.GuncellemeTarihi = DateTime.Now;
                dosya.GuncelleyenKullanici = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dosya açıklaması güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya düzenlenirken hata oluştu");
                return Json(new { success = false, message = "Dosya düzenlenirken bir hata oluştu." });
            }
        }
    }
} 