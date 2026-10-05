using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPortal.Models;
using System.Text.Json;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SidebarMenuController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SidebarMenuController> _logger;
        private readonly WebPortalDbContext _context;

        public SidebarMenuController(IWebHostEnvironment env, ILogger<SidebarMenuController> logger, WebPortalDbContext context)
        {
            _env = env;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var menus = await LoadSidebarMenus();
            return View(menus);
        }

        // AJAX için menü getirme endpoint'i
        [AllowAnonymous]
        public async Task<IActionResult> GetMenu()
        {
            try
            {
                _logger.LogInformation("GetMenu action başladı");
                
                // Tüm aktif menüleri getir
                var allMenus = await _context.SidebarMenus
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync();

                _logger.LogInformation($"Tüm aktif menüler yüklendi: {allMenus.Count}");

                // Her menü için debug bilgisi
                foreach (var menu in allMenus)
                {
                    _logger.LogInformation($"Menu: {menu.Title} (ID: {menu.Id}), IsSubmenu: {menu.IsSubmenu}, ParentId: {menu.ParentId}");
                }

                // Navigation property'leri doldur
                foreach (var menu in allMenus)
                {
                    // Parent referansını doldur
                    if (menu.ParentId.HasValue)
                    {
                        menu.Parent = allMenus.FirstOrDefault(m => m.Id == menu.ParentId.Value);
                        _logger.LogInformation($"  {menu.Title} için parent bulundu: {menu.Parent?.Title} (ID: {menu.Parent?.Id})");
                    }
                    
                    // Alt menüleri bul
                    var submenus = allMenus.Where(m => m.ParentId == menu.Id && m.IsActive).ToList();
                    menu.Submenus = submenus;
                    _logger.LogInformation($"  {menu.Title} için {submenus.Count} alt menü bulundu");
                    
                    if (submenus.Any())
                    {
                        foreach (var sub in submenus)
                        {
                            _logger.LogInformation($"    Alt menü: {sub.Title} (ID: {sub.Id})");
                        }
                    }
                }

                // Sadece ana menüleri döndür (alt menüler zaten Submenus property'sinde)
                var mainMenus = allMenus.Where(m => !m.IsSubmenu).ToList();
                _logger.LogInformation($"Ana menü sayısı: {mainMenus.Count}");

                foreach (var menu in mainMenus)
                {
                    _logger.LogInformation($"Ana menü: {menu.Title}, Submenus count: {menu.Submenus?.Count ?? 0}");
                }

                // JSON serialization öncesi son kontrol
                _logger.LogInformation("JSON serialization başlıyor...");
                var jsonResult = Json(mainMenus);
                _logger.LogInformation("JSON serialization başarılı");
                
                return jsonResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMenu action'da hata oluştu: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, details = ex.StackTrace });
            }
        }

        public async Task<IActionResult> Create()
        {
            var parentMenus = await _context.SidebarMenus
                .Where(m => !m.IsSubmenu)
                .ToListAsync();
            ViewBag.ParentMenus = parentMenus;
            return View(new SidebarMenu());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SidebarMenu menu)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var menus = await _context.SidebarMenus.ToListAsync();
                    
                    // KAPSAMLI DUPLICATE KONTROLÜ
                    var existingMenu = menus.FirstOrDefault(m => 
                        m.Title.Equals(menu.Title, StringComparison.OrdinalIgnoreCase) && 
                        m.ParentId == menu.ParentId);
                    
                    if (existingMenu != null)
                    {
                        ModelState.AddModelError("Title", "Bu başlık zaten kullanılıyor!");
                        ViewBag.ParentMenus = menus.Where(m => !m.IsSubmenu).ToList();
                        return View(menu);
                    }
                    
                    // URL duplicate kontrolü
                    if (!string.IsNullOrEmpty(menu.Url) && menus.Any(m => 
                        m.Url?.Equals(menu.Url, StringComparison.OrdinalIgnoreCase) == true))
                    {
                        ModelState.AddModelError("Url", "Bu URL zaten kullanılıyor!");
                        ViewBag.ParentMenus = menus.Where(m => !m.IsSubmenu).ToList();
                        return View(menu);
                    }
                    
                    // URL otomatik oluştur (eğer boşsa)
                    if (string.IsNullOrEmpty(menu.Url))
                    {
                        var cleanTitle = menu.Title.Replace(" ", "-").Replace("ı", "i").Replace("ğ", "g")
                                                   .Replace("ü", "u").Replace("ş", "s").Replace("ö", "o")
                                                   .Replace("ç", "c").Replace("İ", "I").Replace("Ğ", "G")
                                                   .Replace("Ü", "U").Replace("Ş", "S").Replace("Ö", "O")
                                                   .Replace("Ç", "C");
                        
                        // Alt menü ise parent URL + submenu title formatında oluştur
                        if (menu.ParentId.HasValue)
                        {
                            var parentMenu = menus.Where(m => m.Id == menu.ParentId.Value).FirstOrDefault();
                            if (parentMenu != null)
                            {
                                // Parent menünün URL'ini al (eğer yoksa title'dan oluştur)
                                var parentUrl = parentMenu.Url?.TrimStart('/');
                                if (string.IsNullOrEmpty(parentUrl))
                                {
                                    var parentCleanTitle = parentMenu.Title.Replace(" ", "-").Replace("ı", "i").Replace("ğ", "g")
                                                                       .Replace("ü", "u").Replace("ş", "s").Replace("ö", "o")
                                                                       .Replace("ç", "c").Replace("İ", "I").Replace("Ğ", "G")
                                                                       .Replace("Ü", "U").Replace("Ş", "S").Replace("Ö", "O")
                                                                       .Replace("Ç", "C");
                                    parentUrl = parentCleanTitle.ToLower();
                                }
                                menu.Url = $"/{parentUrl}/{cleanTitle}";
                            }
                            else
                            {
                                menu.Url = $"/{cleanTitle}";
                            }
                        }
                        else
                        {
                            menu.Url = $"/{cleanTitle}";
                        }
                    }
                    
                    // Display order belirle
                    if (menu.ParentId.HasValue)
                    {
                        var parentSubmenus = menus.Where(m => m.ParentId == menu.ParentId).ToList();
                        menu.DisplayOrder = parentSubmenus.Count > 0 ? parentSubmenus.Max(m => m.DisplayOrder) + 1 : 1;
                        menu.IsSubmenu = true;
                    }
                    else
                    {
                        var mainMenus = menus.Where(m => !m.IsSubmenu).ToList();
                        menu.DisplayOrder = mainMenus.Count > 0 ? mainMenus.Max(m => m.DisplayOrder) + 1 : 1;
                        menu.IsSubmenu = false;
                    }
                    
                    menu.CreatedAt = DateTime.Now;
                    menu.CreatedBy = User.Identity?.Name ?? "Admin";
                    menu.UpdatedAt = DateTime.Now;
                    menu.UpdatedBy = User.Identity?.Name ?? "Admin";
                    
                    _context.SidebarMenus.Add(menu);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Menü başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Menü eklenirken hata oluştu: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Menü eklenirken bir hata oluştu: {ex.Message}");
                }
            }
            else
            {
                // Validation hatalarını logla
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation Error: {Error}", error.ErrorMessage);
                }
            }
            
            var parentMenus = await _context.SidebarMenus
                .Where(m => !m.IsSubmenu)
                .ToListAsync();
            ViewBag.ParentMenus = parentMenus;
            return View(menu);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _context.SidebarMenus.FirstOrDefaultAsync(m => m.Id == id);
            
            if (menu == null)
            {
                return NotFound();
            }
            
            var parentMenus = await _context.SidebarMenus
                .Where(m => !m.IsSubmenu && m.Id != id)
                .ToListAsync();
            ViewBag.ParentMenus = parentMenus;
            return View(menu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SidebarMenu menu)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingMenu = await _context.SidebarMenus.FirstOrDefaultAsync(m => m.Id == id);
                    
                    if (existingMenu == null)
                    {
                        return NotFound();
                    }
                    
                    // Güncelleme
                    existingMenu.Title = menu.Title;
                    existingMenu.Url = menu.Url;
                    existingMenu.IconClass = menu.IconClass;
                    existingMenu.RequiredRole = menu.RequiredRole;
                    existingMenu.PageType = menu.PageType;
                    existingMenu.ParentId = menu.ParentId;
                    existingMenu.IsActive = menu.IsActive;
                    existingMenu.UpdatedAt = DateTime.Now;
                    existingMenu.UpdatedBy = User.Identity?.Name ?? "Admin";
                    
                    // Submenu kontrolü
                    if (menu.ParentId.HasValue)
                    {
                        existingMenu.IsSubmenu = true;
                    }
                    else
                    {
                        existingMenu.IsSubmenu = false;
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Menü başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Menü güncellenirken hata oluştu");
                    ModelState.AddModelError("", "Menü güncellenirken bir hata oluştu.");
                }
            }
            
            var parentMenus = await _context.SidebarMenus
                .Where(m => !m.IsSubmenu && m.Id != id)
                .ToListAsync();
            ViewBag.ParentMenus = parentMenus;
            return View(menu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var menu = await _context.SidebarMenus.FirstOrDefaultAsync(m => m.Id == id);
                
                if (menu == null)
                {
                    return Json(new { success = false, message = "Menü bulunamadı." });
                }
                
                // Alt menüleri de sil
                var submenus = await _context.SidebarMenus
                    .Where(m => m.ParentId == id)
                    .ToListAsync();
                
                foreach (var submenu in submenus)
                {
                    _context.SidebarMenus.Remove(submenu);
                }
                
                _context.SidebarMenus.Remove(menu);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Menü başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menü silinirken hata oluştu");
                return Json(new { success = false, message = "Menü silinirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var menu = await _context.SidebarMenus.FirstOrDefaultAsync(m => m.Id == id);
                
                if (menu == null)
                {
                    return Json(new { success = false, message = "Menü bulunamadı." });
                }
                
                menu.IsActive = !menu.IsActive;
                menu.UpdatedAt = DateTime.Now;
                menu.UpdatedBy = User.Identity?.Name ?? "Admin";
                
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = $"Menü {(menu.IsActive ? "aktif" : "pasif")} yapıldı.", isActive = menu.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menü durumu değiştirilirken hata oluştu");
                return Json(new { success = false, message = "Menü durumu değiştirilirken bir hata oluştu." });
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
                    var menu = await _context.SidebarMenus.FirstOrDefaultAsync(m => m.Id == update.Id);
                    if (menu != null)
                    {
                        menu.DisplayOrder = update.Order;
                        menu.UpdatedAt = DateTime.Now;
                        menu.UpdatedBy = User.Identity?.Name ?? "Admin";
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

        private async Task<List<SidebarMenu>> LoadSidebarMenus()
        {
            try
            {
                var menus = await _context.SidebarMenus
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync();
                
                _logger.LogInformation($"LoadSidebarMenus: Toplam {menus.Count} menü bulundu");
                
                // Navigation property'leri doldur
                foreach (var menu in menus)
                {
                    // Parent referansını doldur
                    if (menu.ParentId.HasValue)
                    {
                        menu.Parent = menus.FirstOrDefault(m => m.Id == menu.ParentId.Value);
                    }
                    
                    // Alt menüleri bul
                    menu.Submenus = menus
                        .Where(m => m.ParentId == menu.Id)
                        .OrderBy(m => m.DisplayOrder)
                        .ToList();
                }
                
                return menus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sidebar menüleri yüklenirken hata oluştu");
                return new List<SidebarMenu>();
            }
        }
    }
}
