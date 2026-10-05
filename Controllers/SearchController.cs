using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebPortal.Data;
using WebPortal.Models;

namespace WebPortal.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly WebPortalDbContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(WebPortalDbContext context, ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return View(new SearchResultViewModel { Query = q });
            }

            try
            {
                var searchTerm = q.Trim().ToLowerInvariant();
                _logger.LogInformation("Arama yapılıyor: {SearchTerm}", searchTerm);

                var result = new SearchResultViewModel
                {
                    Query = q,
                    Menus = new List<SidebarMenu>(),
                    Reports = new List<GenericReport>(),
                    Announcements = new List<Duyuru>(),
                    PortalSections = new List<PortalBolum>()
                };

                // Menü arama
                result.Menus = await _context.SidebarMenus
                    .Where(m => m.IsActive &&
                               (m.Title.ToLower().Contains(searchTerm) ||
                                m.Url.ToLower().Contains(searchTerm)))
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync();

                // Rapor arama
                result.Reports = await _context.GenericReports
                    .Where(r => r.IsActive &&
                               (r.Title.ToLower().Contains(searchTerm) ||
                                (r.Description != null && r.Description.ToLower().Contains(searchTerm)) ||
                                r.Section.ToLower().Contains(searchTerm)))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                // Duyuru arama (Baslik/Aciklama)
                result.Announcements = await _context.Duyurular
                    .Where(d => d.IsActive &&
                                (d.Baslik.ToLower().Contains(searchTerm) ||
                                 d.Aciklama.ToLower().Contains(searchTerm)))
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                // Portal bölüm arama (Baslik/Aciklama)
                result.PortalSections = await _context.PortalBolumlar
                    .Where(p => p.IsActive &&
                                (p.Baslik.ToLower().Contains(searchTerm) ||
                                 p.Aciklama.ToLower().Contains(searchTerm)))
                    .OrderBy(p => p.Baslik)
                    .Take(20)
                    .ToListAsync();

                result.TotalResults = result.Menus.Count + result.Reports.Count +
                                     result.Announcements.Count + result.PortalSections.Count;

                _logger.LogInformation("Arama tamamlandı: {TotalResults} sonuç bulundu", result.TotalResults);

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arama sırasında hata oluştu: {Query}", q);
                TempData["Error"] = "Arama sırasında bir hata oluştu.";
                return View(new SearchResultViewModel { Query = q });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Suggest(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            try
            {
                var searchTerm = term.Trim().ToLowerInvariant();
                var suggestions = new List<object>();

                // Menü önerileri
                var menuSuggestions = await _context.SidebarMenus
                    .Where(m => m.IsActive && m.Title.ToLower().Contains(searchTerm))
                    .Select(m => new {
                        type = "menu",
                        title = m.Title,
                        url = m.Url,
                        icon = m.IconClass
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(menuSuggestions);

                // Rapor önerileri
                var reportSuggestions = await _context.GenericReports
                    .Where(r => r.IsActive && r.Title.ToLower().Contains(searchTerm))
                    .Select(r => new {
                        type = "report",
                        title = r.Title,
                        url = $"/DynamicPage/ViewReportFile?id={r.Id}",
                        icon = "fas fa-file-alt"
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(reportSuggestions);

                // Duyuru önerileri
                var announcementSuggestions = await _context.Duyurular
                    .Where(d => d.IsActive && d.Baslik.ToLower().Contains(searchTerm))
                    .Select(d => new {
                        type = "announcement",
                        title = d.Baslik,
                        url = $"/Duyuru/Detay/{d.Id}",
                        icon = "fas fa-bullhorn"
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(announcementSuggestions);

                return Json(suggestions.Take(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öneri getirirken hata oluştu: {Term}", term);
                return Json(new List<object>());
            }
        }
    }

    public class SearchResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public List<SidebarMenu> Menus { get; set; } = new();
        public List<GenericReport> Reports { get; set; } = new();
        public List<Duyuru> Announcements { get; set; } = new();
        public List<PortalBolum> PortalSections { get; set; } = new();
    }
}
