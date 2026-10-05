using WebPortal.Models;

namespace WebPortal.Services;

public interface IMenuService
{
    Task<bool> ProcessExcelFileAndUpdateMenus(IFormFile dosya, string menuType, string yukleyenKullanici);
    Task<List<FoodMenu>> GetFoodMenus();
    Task<List<BreakfastMenu>> GetBreakfastMenus();
    Task<List<ExcelMenuFile>> GetExcelMenuFiles();
    Task<bool> DeleteExcelMenuFile(int id);
    Task<FoodMenu?> GetTodayFoodMenu();
    Task<BreakfastMenu?> GetTodayBreakfastMenu();
}
