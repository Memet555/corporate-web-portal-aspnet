using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebPortal.Middleware
{
    public class DevelopmentUserMiddleware
    {
        private readonly RequestDelegate _next;

        public DevelopmentUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Sahte kullanıcının kimlik ve rollerini oluştur
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "devuser"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.Role, "Finance")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // HttpContext.User'a ata
            context.User = principal;

            // Sonraki middleware'e geç
            await _next(context);
        }
    }
}
