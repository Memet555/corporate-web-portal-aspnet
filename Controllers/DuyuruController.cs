using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using WebPortal.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DuyuruController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly WebPortalDbContext _context;
        private readonly string _uploadPath;

        public DuyuruController(IWebHostEnvironment env, WebPortalDbContext context)
        {
            _env = env;
            _context = context;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "duyurular");
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        // Duyuruları listele
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var duyurular = await _context.Duyurular
                .Include(d => d.Files)
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.Tarih)
                .ToListAsync();
            
            return View(duyurular);
        }

        // Duyuruları listele (List action'ı da ekleyelim)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var duyurular = await _context.Duyurular
                .Include(d => d.Files)
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.Tarih)
                .ToListAsync();
            
            return View("Index", duyurular);
        }

        // Duyuruları JSON olarak getir (AJAX için)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetDuyurular()
        {
            var duyurular = await _context.Duyurular
                .Include(d => d.Files)
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.Tarih)
                .Select(d => new
                {
                    id = d.Id,
                    baslik = d.Baslik,
                    aciklama = d.Aciklama,
                    tarih = d.Tarih,
                    miniFotoIndex = d.MiniFotoIndex,
                    files = d.Files.Select(f => new
                    {
                        id = f.Id,
                        fileName = f.FileName,
                        filePath = f.FilePath,
                        fileType = f.FileType,
                        fileSize = f.FileSize
                    }).ToList()
                })
                .ToListAsync();
            
            return Json(duyurular);
        }

        // Duyuru ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] Duyuru model, List<IFormFile>? fotolar, List<IFormFile>? dosyalar, int? MiniFotoIndex = null)
        {
            try
            {
                // Debug: Model binding'i kontrol et
                System.Diagnostics.Debug.WriteLine($"Model binding - Baslik: {model?.Baslik}, Aciklama: {model?.Aciklama}");
                
                if (model == null)
                {
                    return Json(new { success = false, message = "Model null geldi" });
                }
                
                if (string.IsNullOrEmpty(model.Baslik))
                {
                    return Json(new { success = false, message = "Başlık boş olamaz" });
                }
                
                if (string.IsNullOrEmpty(model.Aciklama))
                {
                    return Json(new { success = false, message = "Açıklama boş olamaz" });
                }
                
                model.Tarih = DateTime.Now;
                model.IsActive = true;
                model.MiniFotoIndex = MiniFotoIndex ?? 0; // Varsayılan olarak ilk fotoğraf
                model.CreatedAt = DateTime.Now;
                model.CreatedBy = User.Identity?.Name ?? "Admin";
                
                // Önce duyuruyu kaydet ki Id alabilelim
                _context.Duyurular.Add(model);
                await _context.SaveChangesAsync();
                
                // Dosya listelerini başlat
                var files = new List<AnnouncementFile>();

                if (fotolar != null && fotolar.Count > 0)
                {
                    const long maxFileSize = 50 * 1024 * 1024; // 50MB
                    foreach (var foto in fotolar)
                    {
                        if (foto != null && foto.Length > 0)
                        {
                            // Dosya boyutu kontrolü
                            if (foto.Length > maxFileSize)
                            {
                                return Json(new { success = false, message = $"Fotoğraf boyutu 50MB'dan büyük olamaz: {foto.FileName}" });
                            }

                            try
                            {
                                var fotoName = Guid.NewGuid() + Path.GetExtension(foto.FileName);
                                var fotoPath = Path.Combine(_uploadPath, fotoName);
                                using (var stream = new FileStream(fotoPath, FileMode.Create))
                                {
                                    await foto.CopyToAsync(stream);
                                }
                                                                 files.Add(new AnnouncementFile
                                 {
                                     DuyuruId = model.Id,
                                     FileType = "Photo",
                                     FileName = foto.FileName,
                                     FilePath = $"/uploads/duyurular/{fotoName}",
                                     FileSize = foto.Length,
                                     UploadedAt = DateTime.Now,
                                     UploadedBy = User.Identity?.Name ?? "Admin"
                                 });
                            }
                            catch (Exception ex)
                            {
                                return Json(new { success = false, message = $"Fotoğraf yüklenirken hata oluştu: {ex.Message}" });
                            }
                        }
                    }
                }
                if (dosyalar != null && dosyalar.Count > 0)
                {
                    const long maxFileSize = 50 * 1024 * 1024; // 50MB
                    foreach (var dosya in dosyalar)
                    {
                        if (dosya != null && dosya.Length > 0)
                        {
                            // Dosya boyutu kontrolü
                            if (dosya.Length > maxFileSize)
                            {
                                return Json(new { success = false, message = $"Dosya boyutu 50MB'dan büyük olamaz: {dosya.FileName}" });
                            }

                            try
                            {
                                var dosyaName = Guid.NewGuid() + Path.GetExtension(dosya.FileName);
                                var dosyaPath = Path.Combine(_uploadPath, dosyaName);
                                using (var stream = new FileStream(dosyaPath, FileMode.Create))
                                {
                                    await dosya.CopyToAsync(stream);
                                }
                                                                 files.Add(new AnnouncementFile
                                 {
                                     DuyuruId = model.Id,
                                     FileType = "Document",
                                     FileName = dosya.FileName,
                                     FilePath = $"/uploads/duyurular/{dosyaName}",
                                     FileSize = dosya.Length,
                                     UploadedAt = DateTime.Now,
                                     UploadedBy = User.Identity?.Name ?? "Admin"
                                 });
                            }
                            catch (Exception ex)
                            {
                                return Json(new { success = false, message = $"Dosya yüklenirken hata oluştu: {ex.Message}" });
                            }
                        }
                    }
                }
                
                // Dosyaları veritabanına kaydet
                if (files.Count > 0)
                {
                    _context.AnnouncementFiles.AddRange(files);
                    await _context.SaveChangesAsync();
                }
                
                return Json(new { success = true, message = "Duyuru başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner Exception: {ex.InnerException.Message}";
                }
                if (ex.InnerException?.InnerException != null)
                {
                    errorMessage += $" | Deep Inner: {ex.InnerException.InnerException.Message}";
                }
                
                // Debug için console'a yazdır
                System.Diagnostics.Debug.WriteLine($"Duyuru ekleme hatası: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Json(new { success = false, message = $"Duyuru eklenirken bir hata oluştu: {errorMessage}" });
            }
        }

        // Duyuru sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var duyuru = await _context.Duyurular
                    .FirstOrDefaultAsync(d => d.Id == id);
                
                if (duyuru == null) 
                {
                    return Json(new { success = false, message = "Duyuru bulunamadı." });
                }
                
                // Dosyaları sil - geçici olarak kaldırıldı
                // if (duyuru.Files != null)
                // {
                //     foreach (var file in duyuru.Files)
                //     {
                //         try
                //         {
                //             var path = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                //             if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                //         }
                //         catch (Exception ex)
                //         {
                //             // Dosya silme hatası olsa bile devam et
                //             System.Diagnostics.Debug.WriteLine($"Dosya silme hatası: {ex.Message}");
                //         }
                //     }
                //     _context.AnnouncementFiles.RemoveRange(duyuru.Files);
                // }

                _context.Duyurular.Remove(duyuru);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Duyuru başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Duyuru silinirken bir hata oluştu: {ex.Message}" });
            }
        }


        // Duyuru düzenleme action'ı
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] Duyuru model, List<IFormFile>? fotolar, List<IFormFile>? dosyalar, int? miniFotoIndex)
        {
            try
            {
                // Debug: Gelen parametreleri kontrol et
                System.Diagnostics.Debug.WriteLine($"=== EDIT ACTION BAŞLADI ===");
                System.Diagnostics.Debug.WriteLine($"Edit - id: {id}");
                System.Diagnostics.Debug.WriteLine($"Edit - model: {model?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Edit - model.Baslik: {model?.Baslik}");
                System.Diagnostics.Debug.WriteLine($"Edit - model.Aciklama: {model?.Aciklama}");
                System.Diagnostics.Debug.WriteLine($"Edit - fotolar: {fotolar?.Count}");
                System.Diagnostics.Debug.WriteLine($"Edit - dosyalar: {dosyalar?.Count}");
                
                // Model null kontrolü
                if (model == null)
                {
                    System.Diagnostics.Debug.WriteLine("Edit - Model null geldi");
                    return Json(new { success = false, message = "Model null geldi" });
                }
                
                var duyuru = await _context.Duyurular
                    .Include(d => d.Files)
                    .FirstOrDefaultAsync(d => d.Id == id);
                
                if (duyuru == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Edit - Duyuru bulunamadı, id: {id}");
                    return Json(new { success = false, message = "Duyuru bulunamadı." });
                }

                // Duyuru bilgilerini güncelle
                duyuru.Baslik = model.Baslik ?? string.Empty;
                duyuru.Aciklama = model.Aciklama ?? string.Empty;
                duyuru.MiniFotoIndex = miniFotoIndex ?? 0;
                duyuru.UpdatedAt = DateTime.Now;
                duyuru.UpdatedBy = User.Identity?.Name ?? "Admin";

                // Yeni fotoğraflar ekle
                if (fotolar != null && fotolar.Count > 0)
                {
                    const long maxFileSize = 50 * 1024 * 1024; // 50MB
                    foreach (var foto in fotolar)
                    {
                        if (foto != null && foto.Length > 0)
                        {
                            if (foto.Length > maxFileSize)
                            {
                                return Json(new { success = false, message = $"Fotoğraf boyutu 50MB'dan büyük olamaz: {foto.FileName}" });
                            }

                            try
                            {
                                var fotoName = Guid.NewGuid() + Path.GetExtension(foto.FileName);
                                var fotoPath = Path.Combine(_uploadPath, fotoName);
                                using (var stream = new FileStream(fotoPath, FileMode.Create))
                                {
                                    await foto.CopyToAsync(stream);
                                }
                                
                                duyuru.Files.Add(new AnnouncementFile
                                {
                                    DuyuruId = duyuru.Id,
                                    FileType = "Photo",
                                    FileName = foto.FileName,
                                    FilePath = $"/uploads/duyurular/{fotoName}",
                                    FileSize = foto.Length,
                                    UploadedAt = DateTime.Now,
                                    UploadedBy = User.Identity?.Name ?? "Admin"
                                });
                            }
                            catch (Exception ex)
                            {
                                return Json(new { success = false, message = $"Fotoğraf yüklenirken hata oluştu: {ex.Message}" });
                            }
                        }
                    }
                }

                // Yeni dosyalar ekle
                if (dosyalar != null && dosyalar.Count > 0)
                {
                    const long maxFileSize = 100 * 1024 * 1024; // 100MB
                    foreach (var dosya in dosyalar)
                    {
                        if (dosya != null && dosya.Length > 0)
                        {
                            if (dosya.Length > maxFileSize)
                            {
                                return Json(new { success = false, message = $"Dosya boyutu 100MB'dan büyük olamaz: {dosya.FileName}" });
                            }

                            try
                            {
                                var dosyaName = Guid.NewGuid() + Path.GetExtension(dosya.FileName);
                                var dosyaPath = Path.Combine(_uploadPath, dosyaName);
                                using (var stream = new FileStream(dosyaPath, FileMode.Create))
                                {
                                    await dosya.CopyToAsync(stream);
                                }
                                
                                duyuru.Files.Add(new AnnouncementFile
                                {
                                    DuyuruId = duyuru.Id,
                                    FileType = "Document",
                                    FileName = dosya.FileName,
                                    FilePath = $"/uploads/duyurular/{dosyaName}",
                                    FileSize = dosya.Length,
                                    UploadedAt = DateTime.Now,
                                    UploadedBy = User.Identity?.Name ?? "Admin"
                                });
                            }
                            catch (Exception ex)
                            {
                                return Json(new { success = false, message = $"Dosya yüklenirken hata oluştu: {ex.Message}" });
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Duyuru başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                // Debug: Detaylı hata bilgisi
                System.Diagnostics.Debug.WriteLine($"Edit hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    if (ex.InnerException.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Deep inner exception: {ex.InnerException.InnerException.Message}");
                    }
                }
                
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                
                return Json(new { success = false, message = $"Duyuru güncellenirken bir hata oluştu: {errorMessage}" });
            }
        }

        // Duyuru detayını gösterecek yeni bir Detay action'ı ekle. Id ile gelen duyuruyu bulup view'a gönder.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            var duyuru = await _context.Duyurular
                .Include(d => d.Files)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (duyuru == null) return NotFound();
            return View(duyuru);
        }

        // Duyuru dosyalarını getir
        [HttpGet]
        public async Task<IActionResult> GetDuyuruFiles(int duyuruId)
        {
            var files = await _context.AnnouncementFiles
                .Where(f => f.DuyuruId == duyuruId)
                .Select(f => new
                {
                    id = f.Id,
                    fileName = f.FileName,
                    filePath = f.FilePath,
                    fileType = f.FileType,
                    fileSize = f.FileSize,
                    uploadedAt = f.UploadedAt,
                    uploadedBy = f.UploadedBy
                })
                .ToListAsync();
            
            return Json(files);
        }

        // Duyuru dosyasını sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDuyuruFile(int fileId)
        {
            try
            {
                var file = await _context.AnnouncementFiles.FindAsync(fileId);
                if (file == null) 
                {
                    return Json(new { success = false, message = "Dosya bulunamadı." });
                }

                // Fiziksel dosyayı sil
                try
                {
                    var path = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fiziksel dosya silme hatası: {ex.Message}");
                }

                // Veritabanından sil
                _context.AnnouncementFiles.Remove(file);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dosya başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Dosya silinirken hata oluştu: {ex.Message}" });
            }
        }

        // Duyuru dosyasını getir
        [HttpGet]
        public async Task<IActionResult> GetDuyuruFile(int fileId)
        {
            var file = await _context.AnnouncementFiles.FindAsync(fileId);
            if (file == null) 
            {
                TempData["ErrorMessage"] = "Dosya bulunamadı.";
                return RedirectToAction(nameof(List));
            }

            // Dosya bilgilerini ViewData ile gönder
            ViewData["FileInfo"] = file;
            return RedirectToAction(nameof(Detay), new { id = file.DuyuruId });
        }
    }
} 