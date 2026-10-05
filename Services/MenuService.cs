using WebPortal.Models;
using WebPortal.Data;
using ClosedXML.Excel;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace WebPortal.Services;

public class MenuService : IMenuService
{
    private readonly WebPortalDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MenuService> _logger;

    public MenuService(WebPortalDbContext context, IWebHostEnvironment env, ILogger<MenuService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    // Excel dosyasının hash'ini hesaplama
    private string CalculateFileHash(string filePath)
    {
        using (var md5 = MD5.Create())
        using (var stream = System.IO.File.OpenRead(filePath))
        {
            var hash = md5.ComputeHash(stream);
            return Convert.ToBase64String(hash);
        }
    }

    public async Task<bool> ProcessExcelFileAndUpdateMenus(IFormFile dosya, string menuType, string yukleyenKullanici)
    {
        try
        {
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", menuType == "Yemek" ? "yemek-listesi" : "kahvalti-listesi");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(dosya.FileName);
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dosya.CopyToAsync(stream);
            }

            // Dosya hash'ini hesapla
            var fileHash = CalculateFileHash(filePath);

            // Eğer aynı hash'e sahip dosya varsa, işlemi tekrarlama
            var existingFile = await _context.ExcelMenuFiles.FirstOrDefaultAsync(f => f.DosyaHash == fileHash && f.Tip == menuType);
            if (existingFile != null)
            {
                // Dosyayı sil çünkü zaten var
                System.IO.File.Delete(filePath);
                return true; // Zaten işlenmiş
            }

            // Excel dosyasından tüm tarihleri oku
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var package = new XLWorkbook(stream))
            {
                if (package.Worksheets == null || package.Worksheets.Count == 0)
                {
                    System.IO.File.Delete(filePath);
                    return false;
                }

                var ws = package.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    System.IO.File.Delete(filePath);
                    return false;
                }
                if (!ws.RowsUsed().Any() || !ws.ColumnsUsed().Any())
                {
                    System.IO.File.Delete(filePath);
                    return false;
                }

                int rowCount = ws.RowsUsed().Count();
                int tarihCol = 1; // 1. sütun Tarih
                int yemek1Col = 3; // 3. sütun Yemek-1
                int yemek2Col = 4; // 4. sütun Yemek-2
                int yemek3Col = 5; // 5. sütun Yemek-3
                int yemek4Col = 6; // 6. sütun Yemek-4

                for (int row = 2; row <= rowCount; row++) // 1. satır başlık
                {
                    var cellTarih = ws.Cell(row, tarihCol).GetValue<string>().Trim();
                    if (string.IsNullOrEmpty(cellTarih)) continue;

                    if (DateTime.TryParse(cellTarih, out DateTime cellDate))
                    {
                        var yemekler = new List<string>();
                        var y1 = ws.Cell(row, yemek1Col).GetValue<string>().Trim();
                        var y2 = ws.Cell(row, yemek2Col).GetValue<string>().Trim();
                        var y3 = ws.Cell(row, yemek3Col).GetValue<string>().Trim();
                        var y4 = ws.Cell(row, yemek4Col).GetValue<string>().Trim();

                        if (!string.IsNullOrEmpty(y1)) yemekler.Add(y1);
                        if (!string.IsNullOrEmpty(y2)) yemekler.Add(y2);
                        if (!string.IsNullOrEmpty(y3)) yemekler.Add(y3);
                        if (!string.IsNullOrEmpty(y4)) yemekler.Add(y4);

                        if (yemekler.Any())
                        {
                            if (menuType == "Yemek")
                            {
                                var existingMenu = await _context.FoodMenus.FirstOrDefaultAsync(m => m.Tarih.Date == cellDate.Date);
                                if (existingMenu != null)
                                {
                                    existingMenu.Corba = yemekler.ElementAtOrDefault(0) ?? "";
                                    existingMenu.AnaYemek1 = yemekler.ElementAtOrDefault(1) ?? "";
                                    existingMenu.AnaYemek2 = yemekler.ElementAtOrDefault(2) ?? "";
                                    existingMenu.Pilav = yemekler.ElementAtOrDefault(3) ?? "";
                                    existingMenu.Tatli = yemekler.ElementAtOrDefault(4) ?? "";
                                    existingMenu.MenuIcerigi = string.Join(", ", yemekler.Where(y => !string.IsNullOrEmpty(y)));
                                    existingMenu.UpdatedAt = DateTime.Now;
                                    existingMenu.UpdatedBy = yukleyenKullanici;
                                }
                                else
                                {
                                    var newMenu = new FoodMenu
                                    {
                                        Tarih = cellDate,
                                        Corba = yemekler.ElementAtOrDefault(0) ?? "",
                                        AnaYemek1 = yemekler.ElementAtOrDefault(1) ?? "",
                                        AnaYemek2 = yemekler.ElementAtOrDefault(2) ?? "",
                                        Pilav = yemekler.ElementAtOrDefault(3) ?? "",
                                        Tatli = yemekler.ElementAtOrDefault(4) ?? "",
                                        MenuIcerigi = string.Join(", ", yemekler.Where(y => !string.IsNullOrEmpty(y))),
                                        IsActive = true,
                                        CreatedAt = DateTime.Now,
                                        CreatedBy = yukleyenKullanici
                                    };
                                    _context.FoodMenus.Add(newMenu);
                                }
                            }
                            else // Kahvalti
                            {
                                var existingMenu = await _context.BreakfastMenus.FirstOrDefaultAsync(m => m.Tarih.Date == cellDate.Date);
                                if (existingMenu != null)
                                {
                                    existingMenu.Zeytin = yemekler.ElementAtOrDefault(0) ?? "";
                                    existingMenu.Peynir = yemekler.ElementAtOrDefault(1) ?? "";
                                    existingMenu.Bal = yemekler.ElementAtOrDefault(2) ?? "";
                                    existingMenu.Kaymak = yemekler.ElementAtOrDefault(3) ?? "";
                                    existingMenu.Yumurta = yemekler.ElementAtOrDefault(4) ?? "";
                                    existingMenu.Domates = yemekler.ElementAtOrDefault(5) ?? "";
                                    existingMenu.Salatalik = yemekler.ElementAtOrDefault(6) ?? "";
                                    existingMenu.MenuIcerigi = string.Join(", ", yemekler.Where(y => !string.IsNullOrEmpty(y)));
                                    existingMenu.UpdatedAt = DateTime.Now;
                                    existingMenu.UpdatedBy = yukleyenKullanici;
                                }
                                else
                                {
                                    var newMenu = new BreakfastMenu
                                    {
                                        Tarih = cellDate,
                                        Zeytin = yemekler.ElementAtOrDefault(0) ?? "",
                                        Peynir = yemekler.ElementAtOrDefault(1) ?? "",
                                        Bal = yemekler.ElementAtOrDefault(2) ?? "",
                                        Kaymak = yemekler.ElementAtOrDefault(3) ?? "",
                                        Yumurta = yemekler.ElementAtOrDefault(4) ?? "",
                                        Domates = yemekler.ElementAtOrDefault(5) ?? "",
                                        Salatalik = yemekler.ElementAtOrDefault(6) ?? "",
                                        MenuIcerigi = string.Join(", ", yemekler.Where(y => !string.IsNullOrEmpty(y))),
                                        IsActive = true,
                                        CreatedAt = DateTime.Now,
                                        CreatedBy = yukleyenKullanici
                                    };
                                    _context.BreakfastMenus.Add(newMenu);
                                }
                            }
                        }
                    }
                }
            }

            // Excel dosyası bilgilerini kaydet
            var excelFile = new ExcelMenuFile
            {
                DosyaAdi = dosya.FileName,
                DosyaYolu = uniqueFileName,
                DosyaHash = fileHash,
                YuklenmeTarihi = DateTime.Now,
                YukleyenKullanici = yukleyenKullanici,
                SonGuncelleme = DateTime.Now,
                Aktif = true,
                Tip = menuType
            };

            // Eski dosyaları pasif yap
            var oldFiles = await _context.ExcelMenuFiles.Where(f => f.Tip == menuType && f.Aktif).ToListAsync();
            foreach (var oldFile in oldFiles)
            {
                oldFile.Aktif = false;
            }

            _context.ExcelMenuFiles.Add(excelFile);

            // Veritabanını kaydet
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel dosyası işleme hatası");
            return false;
        }
    }

    public async Task<List<FoodMenu>> GetFoodMenus()
    {
        return await _context.FoodMenus.Where(m => m.IsActive).OrderByDescending(m => m.Tarih).ToListAsync();
    }

    public async Task<List<BreakfastMenu>> GetBreakfastMenus()
    {
        return await _context.BreakfastMenus.Where(m => m.IsActive).OrderByDescending(m => m.Tarih).ToListAsync();
    }

    public async Task<List<ExcelMenuFile>> GetExcelMenuFiles()
    {
        return await _context.ExcelMenuFiles.Where(f => f.Aktif).OrderByDescending(f => f.YuklenmeTarihi).ToListAsync();
    }

    public async Task<bool> DeleteExcelMenuFile(int id)
    {
        var fileToDelete = await _context.ExcelMenuFiles.FindAsync(id);
        if (fileToDelete == null)
        {
            return false;
        }

        // Dosyayı fiziksel olarak sil
        var filePath = Path.Combine(_env.WebRootPath, "uploads", fileToDelete.Tip == "Yemek" ? "yemek-listesi" : "kahvalti-listesi", fileToDelete.DosyaYolu);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        // Veritabanından sil
        _context.ExcelMenuFiles.Remove(fileToDelete);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<FoodMenu?> GetTodayFoodMenu()
    {
        var today = DateTime.Today;
        return await _context.FoodMenus.FirstOrDefaultAsync(m => m.Tarih.Date == today && m.IsActive);
    }

    public async Task<BreakfastMenu?> GetTodayBreakfastMenu()
    {
        var today = DateTime.Today;
        return await _context.BreakfastMenus.FirstOrDefaultAsync(m => m.Tarih.Date == today && m.IsActive);
    }
}
