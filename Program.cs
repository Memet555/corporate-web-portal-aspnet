using Microsoft.AspNetCore.Authentication.Cookies;
using WebPortal.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.EntityFrameworkCore;
using WebPortal.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IUserService, InMemoryUserService>();
builder.Services.AddScoped<IMenuService, MenuService>();

// DbContext servisini ekle
builder.Services.AddDbContext<WebPortalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.AccessDeniedPath = "/Login/AccessDenied"; // Yetkisiz erişim için yönlendirme
    });

builder.Services.AddHttpContextAccessor(); // HttpContext'e erişim için
builder.Services.AddControllersWithViews(options =>
{
    // Dosya yükleme limitlerini artır
    options.MaxModelBindingCollectionSize = 1000;
    options.MaxModelBindingRecursionDepth = 100;
});

// Dosya yükleme limitlerini artır
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
    options.ValueLengthLimit = 52428800; // 50MB
    options.MemoryBufferThreshold = 52428800; // 50MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Kimlik doğrulama middleware'ini etkinleştir
app.UseAuthorization(); // Yetkilendirme middleware'ini etkinleştir

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Tüm dinamik sayfalar için catch-all routing (ugur, ugur/aas, ikk gibi)
app.MapControllerRoute(
    name: "catchAll",
    pattern: "{*url}",
    defaults: new { controller = "DynamicPage", action = "Show" });

// Seed data ekle
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WebPortalDbContext>();
    DbInitializer.Initialize(context);
}

app.Run();
