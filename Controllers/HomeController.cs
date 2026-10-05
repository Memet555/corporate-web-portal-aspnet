using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebPortal.Models;
using WebPortal.Services;
using WebPortal.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace WebPortal.Controllers
{
	[AllowAnonymous]
	public class HomeController : Controller
	{
		private readonly IMenuService _menuService;
		private readonly WebPortalDbContext _context;
		private readonly ILogger<HomeController> _logger;

		public HomeController(IMenuService menuService, WebPortalDbContext context, ILogger<HomeController> logger)
		{
			_menuService = menuService;
			_context = context;
			_logger = logger;
		}

		public async Task<IActionResult> Index()
		{
			try
			{
				// Ana sayfa verilerini veritabanından al
				var viewModel = new HomeViewModel
				{
					Duyurular = await _context.Duyurular
						.Include(d => d.Files)
						.Where(d => d.IsActive)
						.OrderByDescending(d => d.Tarih)
						.Take(5)
						.ToListAsync(),
					
					YonetimMesaji = await _context.YonetimMesajlari
						.Where(y => y.IsActive)
						.OrderByDescending(y => y.Tarih)
						.FirstOrDefaultAsync(),
					
					MissionVision = await _context.MissionVision
						.Where(m => m.IsActive)
						.FirstOrDefaultAsync()
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ana sayfa yüklenirken hata oluştu");
				return View(new HomeViewModel());
			}
		}

		public async Task<IActionResult> YonetimMesaji()
		{
			try
			{
				var yonetimMesajlari = await _context.YonetimMesajlari
					.Include(y => y.Dosyalar)
					.Where(y => y.IsActive)
					.OrderByDescending(y => y.Tarih)
					.ToListAsync();

				return View(yonetimMesajlari);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yönetim mesajları yüklenirken hata oluştu");
				return View(new List<YonetimMesaji>());
			}
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
		}

		// Yemek menüleri
		public async Task<IActionResult> YemekMenusu()
		{
			try
			{
				var yemekMenusu = await _context.FoodMenus
					.Where(f => f.IsActive)
					.OrderByDescending(f => f.Tarih)
					.FirstOrDefaultAsync();

				if (yemekMenusu == null)
				{
					return View(new FoodMenu { Tarih = DateTime.Today });
				}

				return View(yemekMenusu);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yemek menüsü yüklenirken hata oluştu");
				return View(new FoodMenu { Tarih = DateTime.Today });
			}
		}

		public async Task<IActionResult> KahvaltiMenusu()
		{
			try
			{
				var kahvaltiMenusu = await _context.BreakfastMenus
					.Where(b => b.IsActive)
					.OrderByDescending(b => b.Tarih)
					.FirstOrDefaultAsync();

				if (kahvaltiMenusu == null)
				{
					return View(new BreakfastMenu { Tarih = DateTime.Today });
				}

				return View(kahvaltiMenusu);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Kahvaltı menüsü yüklenirken hata oluştu");
				return View(new BreakfastMenu { Tarih = DateTime.Today });
			}
		}

		// Admin için yemek menüsü ekleme/düzenleme
		[Authorize(Roles = "Admin")]
		public IActionResult YemekMenusuEkle()
		{
			return View(new FoodMenu());
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> YemekMenusuEkle(FoodMenu model)
		{
			if (ModelState.IsValid)
			{
				try
				{
					model.IsActive = true;
					model.CreatedAt = DateTime.Now;
					model.CreatedBy = User.Identity?.Name ?? "Admin";

					_context.FoodMenus.Add(model);
					await _context.SaveChangesAsync();

					TempData["Success"] = "Yemek menüsü başarıyla eklendi.";
					return RedirectToAction(nameof(YemekMenusu));
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Yemek menüsü eklenirken hata oluştu");
					ModelState.AddModelError("", "Yemek menüsü eklenirken bir hata oluştu.");
				}
			}

			return View(model);
		}

		// Yönetim mesajı ekleme
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddYonetimMesaji([FromForm] YonetimMesaji model)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(model.Mesaj))
				{
					return Json(new { success = false, message = "Mesaj alanı zorunludur." });
				}

				model.IsActive = true;
				model.CreatedAt = DateTime.Now;
				model.CreatedBy = User.Identity?.Name ?? "Admin";
				model.UpdatedAt = DateTime.Now;
				model.UpdatedBy = User.Identity?.Name ?? "Admin";
				model.GecerlilikTarihi = model.Tarih.AddDays(30); // Varsayılan olarak 30 gün geçerli

				_context.YonetimMesajlari.Add(model);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Yönetim mesajı başarıyla eklendi." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yönetim mesajı eklenirken hata oluştu");
				return Json(new { success = false, message = "Yönetim mesajı eklenirken bir hata oluştu." });
			}
		}

		// Yönetim mesajı düzenleme
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditYonetimMesaji([FromForm] YonetimMesaji model)
		{
			try
			{
				var existingMesaj = await _context.YonetimMesajlari.FindAsync(model.Id);
				if (existingMesaj == null)
				{
					return Json(new { success = false, message = "Yönetim mesajı bulunamadı." });
				}

				existingMesaj.Mesaj = model.Mesaj;
				existingMesaj.Tarih = model.Tarih;
				existingMesaj.Baslik = model.Baslik;
				existingMesaj.UpdatedAt = DateTime.Now;
				existingMesaj.UpdatedBy = User.Identity?.Name ?? "Admin";

				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Yönetim mesajı başarıyla güncellendi." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yönetim mesajı güncellenirken hata oluştu");
				return Json(new { success = false, message = "Yönetim mesajı güncellenirken bir hata oluştu." });
			}
		}

		// Yönetim mesajı silme
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteYonetimMesaji(int id)
		{
			try
			{
				var mesaj = await _context.YonetimMesajlari.FindAsync(id);
				if (mesaj == null)
				{
					return Json(new { success = false, message = "Yönetim mesajı bulunamadı." });
				}

				mesaj.IsActive = false;
				mesaj.UpdatedAt = DateTime.Now;
				mesaj.UpdatedBy = User.Identity?.Name ?? "Admin";

				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Yönetim mesajı başarıyla silindi." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yönetim mesajı silinirken hata oluştu");
				return Json(new { success = false, message = "Yönetim mesajı silinirken bir hata oluştu." });
			}
		}

		// Yönetim mesajı dosya ekleme
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddYonetimMesajiFile([FromForm] YonetimMesajiDosya model, IFormFile file)
		{
			try
			{
				if (file == null || file.Length == 0)
				{
					return Json(new { success = false, message = "Dosya seçilmedi." });
				}

				if (model.YonetimMesajiId <= 0)
				{
					return Json(new { success = false, message = "Geçersiz yönetim mesajı ID'si." });
				}

				var yonetimMesaji = await _context.YonetimMesajlari.FindAsync(model.YonetimMesajiId);
				if (yonetimMesaji == null)
				{
					return Json(new { success = false, message = "Yönetim mesajı bulunamadı." });
				}

				// Dosya yükleme klasörünü oluştur
				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "yonetim-mesajlari");
				if (!Directory.Exists(uploadsFolder))
				{
					Directory.CreateDirectory(uploadsFolder);
				}

				// Dosya adını oluştur
				var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
				var filePath = Path.Combine(uploadsFolder, fileName);

				// Dosyayı kaydet
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				// Veritabanına kaydet
				var yonetimMesajiDosya = new YonetimMesajiDosya
				{
					YonetimMesajiId = model.YonetimMesajiId,
					DosyaAdi = file.FileName,
					DosyaYolu = fileName,
					DosyaBoyutu = file.Length,
					YuklenmeTarihi = DateTime.Now,
					YukleyenKullanici = User.Identity?.Name ?? "Admin",
					Aciklama = model.Aciklama,
					FileType = GetFileType(file.FileName)
				};

				_context.YonetimMesajiDosyalar.Add(yonetimMesajiDosya);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Dosya başarıyla eklendi." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yönetim mesajı dosyası eklenirken hata oluştu");
				return Json(new { success = false, message = "Dosya eklenirken bir hata oluştu." });
			}
		}

		// Yönetim mesajı dosya düzenleme
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditYonetimMesajiFile([FromForm] YonetimMesajiDosya model, IFormFile? file)
		{
			try
			{
				var dosya = await _context.YonetimMesajiDosyalar.FindAsync(model.Id);
				if (dosya == null)
				{
					return Json(new { success = false, message = "Dosya bulunamadı." });
				}

				// Yeni dosya yüklendiyse
				if (file != null && file.Length > 0)
				{
					// Eski dosyayı sil
					var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "yonetim-mesajlari", dosya.DosyaYolu);
					if (System.IO.File.Exists(oldFilePath))
					{
						System.IO.File.Delete(oldFilePath);
					}

					// Yeni dosyayı yükle
					var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "yonetim-mesajlari");
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
					dosya.DosyaBoyutu = file.Length;
					dosya.FileType = GetFileType(file.FileName);
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
				_logger.LogError(ex, "Yönetim mesajı dosyası güncellenirken hata oluştu");
				return Json(new { success = false, message = "Dosya güncellenirken bir hata oluştu." });
			}
		}

		// Yönetim mesajı dosya silme
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteYonetimMesajiFile(int id)
		{
			try
			{
				var dosya = await _context.YonetimMesajiDosyalar.FindAsync(id);
				if (dosya == null)
				{
					return Json(new { success = false, message = "Dosya bulunamadı." });
				}

				// Fiziksel dosyayı sil
				var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "yonetim-mesajlari", dosya.DosyaYolu);
				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}

				_context.YonetimMesajiDosyalar.Remove(dosya);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Dosya başarıyla silindi." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yönetim mesajı dosyası silinirken hata oluştu");
				return Json(new { success = false, message = "Dosya silinirken bir hata oluştu." });
			}
		}

		// Yönetim mesajı dosya bilgilerini getir
		[HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> GetYonetimMesajiFile(int id)
		{
			try
			{
				var dosya = await _context.YonetimMesajiDosyalar.FindAsync(id);
				if (dosya == null)
				{
					return Json(new { success = false, message = "Dosya bulunamadı." });
				}

				return Json(new { 
					success = true, 
					file = new {
						id = dosya.Id,
						fileName = dosya.DosyaYolu,
						originalFileName = dosya.DosyaAdi,
						description = dosya.Aciklama,
						yonetimMesajiId = dosya.YonetimMesajiId
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yönetim mesajı dosya bilgileri getirilirken hata oluştu");
				return Json(new { success = false, message = "Dosya bilgileri getirilirken hata oluştu." });
			}
		}

		// Dosya tipini belirle
		private string GetFileType(string fileName)
		{
			var extension = Path.GetExtension(fileName).ToLowerInvariant();
			return extension switch
			{
				".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "Image",
				".pdf" => "PDF",
				".doc" or ".docx" => "Word",
				".xls" or ".xlsx" => "Excel",
				".zip" or ".rar" => "Archive",
				_ => "Document"
			};
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> YemekMenusuDuzenle(int id)
		{
			var yemekMenusu = await _context.FoodMenus.FirstOrDefaultAsync(f => f.Id == id);
			if (yemekMenusu == null)
			{
				return NotFound();
			}

			return View(yemekMenusu);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> YemekMenusuDuzenle(int id, FoodMenu model)
		{
			if (ModelState.IsValid)
			{
				try
				{
					var yemekMenusu = await _context.FoodMenus.FirstOrDefaultAsync(f => f.Id == id);
					if (yemekMenusu == null)
					{
						return NotFound();
					}

					yemekMenusu.Tarih = model.Tarih;
					yemekMenusu.Corba = model.Corba;
					yemekMenusu.AnaYemek1 = model.AnaYemek1;
					yemekMenusu.AnaYemek2 = model.AnaYemek2;
					yemekMenusu.Pilav = model.Pilav;
					yemekMenusu.Tatli = model.Tatli;
					yemekMenusu.UpdatedAt = DateTime.Now;
					yemekMenusu.UpdatedBy = User.Identity?.Name ?? "Admin";

					await _context.SaveChangesAsync();

					TempData["Success"] = "Yemek menüsü başarıyla güncellendi.";
					return RedirectToAction(nameof(YemekMenusu));
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Yemek menüsü güncellenirken hata oluştu");
					ModelState.AddModelError("", "Yemek menüsü güncellenirken bir hata oluştu.");
				}
			}

			return View(model);
		}

		// Admin için kahvaltı menüsü ekleme/düzenleme
		[Authorize(Roles = "Admin")]
		public IActionResult KahvaltiMenusuEkle()
		{
			return View(new BreakfastMenu());
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> KahvaltiMenusuEkle(BreakfastMenu model)
		{
			if (ModelState.IsValid)
			{
				try
				{
					model.IsActive = true;
					model.CreatedAt = DateTime.Now;
					model.CreatedBy = User.Identity?.Name ?? "Admin";

					_context.BreakfastMenus.Add(model);
					await _context.SaveChangesAsync();

					TempData["Success"] = "Kahvaltı menüsü başarıyla eklendi.";
					return RedirectToAction(nameof(KahvaltiMenusu));
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Kahvaltı menüsü eklenirken hata oluştu");
					ModelState.AddModelError("", "Kahvaltı menüsü eklenirken bir hata oluştu.");
				}
			}

			return View(model);
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> KahvaltiMenusuDuzenle(int id)
		{
			var kahvaltiMenusu = await _context.BreakfastMenus.FirstOrDefaultAsync(b => b.Id == id);
			if (kahvaltiMenusu == null)
			{
				return NotFound();
			}

			return View(kahvaltiMenusu);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> KahvaltiMenusuDuzenle(int id, BreakfastMenu model)
		{
			if (ModelState.IsValid)
			{
				try
				{
					var kahvaltiMenusu = await _context.BreakfastMenus.FirstOrDefaultAsync(b => b.Id == id);
					if (kahvaltiMenusu == null)
					{
						return NotFound();
					}

					kahvaltiMenusu.Tarih = model.Tarih;
					kahvaltiMenusu.Zeytin = model.Zeytin;
					kahvaltiMenusu.Peynir = model.Peynir;
					kahvaltiMenusu.Bal = model.Bal;
					kahvaltiMenusu.Kaymak = model.Kaymak;
					kahvaltiMenusu.Yumurta = model.Yumurta;
					kahvaltiMenusu.Domates = model.Domates;
					kahvaltiMenusu.Salatalik = model.Salatalik;
					kahvaltiMenusu.UpdatedAt = DateTime.Now;
					kahvaltiMenusu.UpdatedBy = User.Identity?.Name ?? "Admin";

					await _context.SaveChangesAsync();

					TempData["Success"] = "Kahvaltı menüsü başarıyla güncellendi.";
					return RedirectToAction(nameof(KahvaltiMenusu));
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Kahvaltı menüsü güncellenirken hata oluştu");
					ModelState.AddModelError("", "Kahvaltı menüsü güncellenirken bir hata oluştu.");
				}
			}

			return View(model);
		}

		// ======= AJAX/JSON ENDPOINTS =======
		

		
		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetPortalSections()
		{
			var sections = await _context.PortalBolumlar
				.Where(b => b.IsActive)
				.OrderBy(b => b.DisplayOrder)
				.Select(b => new
				{
					id = b.Id,
					name = string.IsNullOrWhiteSpace(b.Name) ? b.Baslik : b.Name,
					description = string.IsNullOrWhiteSpace(b.Description) ? b.Aciklama : b.Description
				})
				.ToListAsync();
			return Json(sections);
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetMissionVision()
		{
			var mv = await _context.MissionVision.Where(x => x.IsActive).FirstOrDefaultAsync();
			return Json(new
			{
				misyon = mv?.Misyon ?? string.Empty,
				vizyon = mv?.Vizyon ?? string.Empty
			});
		}

		public class MissionVisionUpdateDto
		{
			public string misyon { get; set; } = string.Empty;
			public string vizyon { get; set; } = string.Empty;
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateMissionVision([FromBody] MissionVisionUpdateDto dto)
		{
			try
			{
				var mv = await _context.MissionVision.FirstOrDefaultAsync();
				if (mv == null)
				{
					mv = new MissionVision
					{
						Misyon = dto.misyon,
						Vizyon = dto.vizyon,
						IsActive = true,
						CreatedAt = DateTime.Now,
						CreatedBy = User.Identity?.Name ?? "Admin",
						GuncellemeTarihi = DateTime.Now,
						GuncelleyenKullanici = User.Identity?.Name ?? "Admin"
					};
					_context.MissionVision.Add(mv);
				}
				else
				{
					mv.Misyon = dto.misyon;
					mv.Vizyon = dto.vizyon;
					mv.UpdatedAt = DateTime.Now;
					mv.UpdatedBy = User.Identity?.Name ?? "Admin";
					mv.GuncellemeTarihi = DateTime.Now;
					mv.GuncelleyenKullanici = User.Identity?.Name ?? "Admin";
				}

				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Mission/Vision güncellenirken hata oluştu");
				return Json(new { success = false, message = "Güncelleme sırasında hata oluştu." });
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetTodayFoodMenu()
		{
			var today = DateTime.Today;
			var menu = await _menuService.GetTodayFoodMenu();
			
			var gunAdi = GetTurkishDayName(today);
			
			return Json(new
			{
				success = menu != null,
				tarih = today.ToString("yyyy-MM-dd"),
				gunAdi = gunAdi,
				menu = menu == null ? null : new
				{
					menu.Id,
					menu.MenuIcerigi,
					menu.Corba,
					menu.AnaYemek1,
					menu.AnaYemek2,
					menu.Pilav,
					menu.Tatli,
					dosyaYolu = menu.DosyaYolu,
					yukleyenKullanici = menu.YukleyenKullanici
				}
			});
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetTodayBreakfastMenu()
		{
			var today = DateTime.Today;
			var menu = await _menuService.GetTodayBreakfastMenu();
			
			var gunAdi = GetTurkishDayName(today);
			
			return Json(new
			{
				success = menu != null,
				tarih = today.ToString("yyyy-MM-dd"),
				gunAdi = gunAdi,
				menu = menu == null ? null : new
				{
					menu.Id,
					menu.MenuIcerigi,
					menu.Zeytin,
					menu.Peynir,
					menu.Bal,
					menu.Kaymak,
					menu.Yumurta,
					menu.Domates,
					menu.Salatalik,
					dosyaYolu = menu.DosyaYolu,
					yukleyenKullanici = menu.YukleyenKullanici
				}
			});
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadFoodMenu(DateTime tarih, IFormFile dosya)
		{
			try
			{
				if (dosya == null || dosya.Length == 0)
				{
					return Json(new { success = false, message = "Dosya seçilmedi." });
				}

				// MenuService kullanarak Excel dosyasını işle
				var result = await _menuService.ProcessExcelFileAndUpdateMenus(dosya, "Yemek", User.Identity?.Name ?? "Admin");
				
				if (result)
				{
					return Json(new { success = true, message = "Yemek listesi başarıyla yüklendi ve işlendi." });
				}
				else
				{
					return Json(new { success = false, message = "Excel dosyası işlenirken hata oluştu." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Yemek listesi yüklenirken hata oluştu");
				return Json(new { success = false, message = "Yükleme sırasında hata oluştu." });
			}
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UploadBreakfastMenu(DateTime tarih, IFormFile dosya)
		{
			try
			{
				if (dosya == null || dosya.Length == 0)
				{
					return Json(new { success = false, message = "Dosya seçilmedi." });
				}

				// MenuService kullanarak Excel dosyasını işle
				var result = await _menuService.ProcessExcelFileAndUpdateMenus(dosya, "Kahvalti", User.Identity?.Name ?? "Admin");
				
				if (result)
				{
					return Json(new { success = true, message = "Kahvaltı listesi başarıyla yüklendi ve işlendi." });
				}
				else
				{
					return Json(new { success = false, message = "Excel dosyası işlenirken hata oluştu." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Kahvaltı listesi yüklenirken hata oluştu");
				return Json(new { success = false, message = "Yükleme sırasında hata oluştu." });
			}
		}

		private static string GetTurkishDayName(DateTime date)
		{
			var culture = new CultureInfo("tr-TR");
			return culture.DateTimeFormat.GetDayName(date.DayOfWeek);
		}
	}

	public class HomeViewModel
	{
		public List<Duyuru> Duyurular { get; set; } = new();
		public YonetimMesaji? YonetimMesaji { get; set; }
		public MissionVision? MissionVision { get; set; }
	}
}
