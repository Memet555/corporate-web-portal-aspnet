using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Models;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PortalBolumController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<PortalBolumController> _logger;

        public PortalBolumController(WebPortalDbContext context, ILogger<PortalBolumController> logger)
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

        public async Task<IActionResult> Edit(int id)
        {
            var bolum = await _context.PortalBolumlar
                .Include(b => b.Dosyalar)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bolum == null)
            {
                return NotFound();
            }

            return View(bolum);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PortalBolum model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var bolum = await _context.PortalBolumlar
                        .Include(b => b.Dosyalar)
                        .FirstOrDefaultAsync(b => b.Id == id);

                    if (bolum == null)
                    {
                        return NotFound();
                    }

                    bolum.Baslik = model.Baslik;
                    bolum.Aciklama = model.Aciklama;
                    bolum.IconClass = model.IconClass;
                    bolum.IsActive = model.IsActive;
                    bolum.UpdatedAt = DateTime.Now;
                    bolum.UpdatedBy = User.Identity?.Name ?? "Admin";

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Portal bölümü başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Portal bölümü güncellenirken hata oluştu");
                    ModelState.AddModelError("", "Portal bölümü güncellenirken bir hata oluştu.");
                }
            }

            return View(model);
        }

        // Portal bölümü detay sayfası
        [AllowAnonymous]
        public async Task<IActionResult> Detay(int id)
        {
            try
            {
                var bolum = await _context.PortalBolumlar
                    .Include(b => b.Dosyalar)
                    .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

                if (bolum == null)
                {
                    return NotFound("Portal bölümü bulunamadı.");
                }

                ViewData["PortalBolumId"] = bolum.Id;
                return View(bolum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portal bölümü detayı yüklenirken hata oluştu");
                return StatusCode(500, "Portal bölümü detayı yüklenirken hata oluştu.");
            }
        }

        // Portal bölümüne dosya ekleme
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] PortalBolumDosya model, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Dosya seçilmedi." });
                }

                if (model.PortalBolumId <= 0)
                {
                    return Json(new { success = false, message = "Geçersiz portal bölümü ID'si." });
                }

                var bolum = await _context.PortalBolumlar.FindAsync(model.PortalBolumId);
                if (bolum == null)
                {
                    return Json(new { success = false, message = "Portal bölümü bulunamadı." });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "portal-bolumler");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var dosya = new PortalBolumDosya
                {
                    PortalBolumId = model.PortalBolumId,
                    SectionName = model.SectionName,
                    FileName = fileName,
                    DosyaAdi = file.FileName,
                    DosyaYolu = fileName,
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description,
                    UploadDate = DateTime.Now,
                    YuklenmeTarihi = DateTime.Now,
                    YukleyenKullanici = User.Identity?.Name ?? "Admin",
                    GuncellemeTarihi = DateTime.Now,
                    GuncelleyenKullanici = User.Identity?.Name ?? "Admin"
                };

                _context.PortalBolumDosyalar.Add(dosya);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dosya başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya eklenirken hata oluştu");
                return Json(new { success = false, message = "Dosya eklenirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBolum(int id)
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

                var filePath = Path.Combine(uploadPath, Path.GetFileName(dosyaModel.DosyaYolu));
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

        // Dosya düzenleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] PortalBolumDosya model, IFormFile file)
        {
            try
            {
                var dosya = await _context.PortalBolumDosyalar.FindAsync(model.Id);
                if (dosya == null)
                {
                    return Json(new { success = false, message = "Dosya bulunamadı." });
                }

                // Yeni dosya yüklendiyse
                if (file != null && file.Length > 0)
                {
                    // Eski dosyayı sil
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "portal-bolumler", dosya.DosyaYolu);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    // Yeni dosyayı yükle
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "portal-bolumler");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    dosya.DosyaYolu = fileName;
                    dosya.DosyaAdi = file.FileName;
                }

                // Açıklamayı güncelle
                dosya.Aciklama = model.Aciklama;
                dosya.GuncellemeTarihi = DateTime.Now;
                dosya.GuncelleyenKullanici = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dosya başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya güncellenirken hata oluştu");
                return Json(new { success = false, message = "Dosya güncellenirken bir hata oluştu." });
            }
        }

        // Dosya silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var dosya = await _context.PortalBolumDosyalar.FindAsync(id);
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

        // Dosya listesi (AJAX için)
        [HttpGet]
        public async Task<IActionResult> List(string section)
        {
            try
            {
                var bolum = await _context.PortalBolumlar
                    .Include(b => b.Dosyalar)
                    .FirstOrDefaultAsync(b => b.Baslik == section && b.IsActive);

                if (bolum == null)
                {
                    return PartialView("~/Views/Shared/_PortalBolumDosyaListPartial.cshtml", new List<PortalBolumDosya>());
                }

                ViewData["SectionName"] = bolum.Baslik;
                ViewData["PortalBolumId"] = bolum.Id;
                return PartialView("~/Views/Shared/_PortalBolumDosyaListPartial.cshtml", bolum.Dosyalar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya listesi yüklenirken hata oluştu");
                return PartialView("~/Views/Shared/_PortalBolumDosyaListPartial.cshtml", new List<PortalBolumDosya>());
            }
        }

        // Dosya bilgilerini getir (düzenleme için)
        [HttpGet]
        public async Task<IActionResult> GetFile(int id)
        {
            try
            {
                var dosya = await _context.PortalBolumDosyalar.FindAsync(id);
                if (dosya == null)
                {
                    return Json(new { success = false, message = "Dosya bulunamadı." });
                }

                return Json(new { 
                    success = true, 
                    file = new {
                        id = dosya.Id,
                        fileName = dosya.DosyaYolu,
                        description = dosya.Aciklama,
                        sectionName = dosya.SectionName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya bilgileri getirilirken hata oluştu");
                return Json(new { success = false, message = "Dosya bilgileri getirilirken hata oluştu." });
            }
        }
    }
}
