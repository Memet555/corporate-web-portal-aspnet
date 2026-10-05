
namespace WebPortal.Models;

public class ExcelDosya
{
    public int Id { get; set; }
    public string? DosyaAdi { get; set; }
    public string? Aciklama { get; set; }
    public string? DosyaYolu { get; set; }
    public DateTime YuklenmeTarihi { get; set; }
}
