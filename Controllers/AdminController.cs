using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebPortal.Data;
using WebPortal.Models;
using System.Security.Cryptography;
using System.Text;

namespace WebPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(WebPortalDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _context.Users
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar yüklenirken hata oluştu");
                TempData["Error"] = "Kullanıcılar yüklenirken bir hata oluştu.";
                return View(new List<User>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Kullanıcı adı kontrolü
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username);

                    if (existingUser != null)
                    {
                        TempData["Error"] = "Bu kullanıcı adı zaten kullanılıyor.";
                        return RedirectToAction("Index");
                    }

                    var user = new User
                    {
                        Username = model.Username,
                        Password = HashPassword(model.Password),
                        Role = model.Role,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = User.Identity?.Name ?? "Admin",
                        UpdatedAt = DateTime.Now,
                        UpdatedBy = User.Identity?.Name ?? "Admin"
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Yeni kullanıcı oluşturuldu: {Username} - {Role}", user.Username, user.Role);
                    TempData["Success"] = $"Kullanıcı '{user.Username}' başarıyla oluşturuldu.";
                }
                else
                {
                    TempData["Error"] = "Lütfen tüm alanları doğru şekilde doldurun.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı oluşturulurken hata oluştu: {Username}", model.Username);
                TempData["Error"] = "Kullanıcı oluşturulurken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Index");
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                var status = user.IsActive ? "aktif" : "pasif";
                _logger.LogInformation("Kullanıcı durumu değiştirildi: {Username} - {Status}", user.Username, status);
                TempData["Success"] = $"Kullanıcı '{user.Username}' {status} yapıldı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı durumu değiştirilirken hata oluştu: {Id}", id);
                TempData["Error"] = "Kullanıcı durumu değiştirilirken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Kendini silmeyi engelle
                if (user.Username == User.Identity?.Name)
                {
                    TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
                    return RedirectToAction("Index");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Kullanıcı silindi: {Username}", user.Username);
                TempData["Success"] = $"Kullanıcı '{user.Username}' başarıyla silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu: {Id}", id);
                TempData["Error"] = "Kullanıcı silinirken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    TempData["Error"] = "Şifre en az 6 karakter olmalıdır.";
                    return RedirectToAction("Index");
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Index");
                }

                user.Password = HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = User.Identity?.Name ?? "Admin";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Kullanıcı şifresi sıfırlandı: {Username}", user.Username);
                TempData["Success"] = $"Kullanıcı '{user.Username}' şifresi sıfırlandı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlanırken hata oluştu: {Id}", id);
                TempData["Error"] = "Şifre sıfırlanırken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }

    public class CreateUserViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }
}