using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WebPortal.Models;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace WebPortal.Controllers
{
    public class LoginController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(WebPortalDbContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

                    if (user != null && VerifyPassword(model.Password, user.Password))
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Role, user.Role),
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // 8 saat oturum
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // Son giriş tarihini güncelle
                        // LastLoginAt artık gerekli değil
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Kullanıcı giriş yaptı: {Username}", user.Username);

                        // Admin ise admin paneline, değilse ana sayfaya yönlendir
                        if (user.Role == "Admin")
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
                        _logger.LogWarning("Başarısız giriş denemesi: {Username}", model.Username);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Giriş işlemi sırasında hata oluştu");
                    ModelState.AddModelError("", "Giriş işlemi sırasında bir hata oluştu.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("Kullanıcı çıkış yaptı: {Username}", User.Identity?.Name);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çıkış işlemi sırasında hata oluştu");
                return RedirectToAction("Index", "Home");
            }
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            try
            {
                // Hash'lenmiş şifreyi doğrula
                var hashedInput = HashPassword(inputPassword);
                return hashedInput == storedPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre doğrulama sırasında hata oluştu");
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Şifre değiştirme sayfası
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var username = User.Identity?.Name;
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                    if (user == null)
                    {
                        ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                        return View(model);
                    }

                    // Mevcut şifreyi doğrula
                    if (!VerifyPassword(model.CurrentPassword, user.Password))
                    {
                        ModelState.AddModelError("CurrentPassword", "Mevcut şifre hatalı.");
                        return View(model);
                    }

                    // Yeni şifreyi hash'le ve kaydet
                    user.Password = HashPassword(model.NewPassword);
                    user.UpdatedAt = DateTime.Now;
                    user.UpdatedBy = username;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Şifreniz başarıyla değiştirildi.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Şifre değiştirme sırasında hata oluştu");
                    ModelState.AddModelError("", "Şifre değiştirme sırasında bir hata oluştu.");
                }
            }

            return View(model);
        }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Şifre")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre gereklidir.")]
        [StringLength(100, ErrorMessage = "{0} en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre Tekrar")]
        [Compare("NewPassword", ErrorMessage = "Yeni şifre ve tekrarı eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
