using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DATN.api.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AntiforgeryController : ControllerBase
    {
        [HttpGet("token")]
        [IgnoreAntiforgeryToken] // MUST ignore validation on the endpoint that serves the token
        public IActionResult GetToken([FromServices] IAntiforgery antiforgery)
        {
            var tokens = antiforgery.GetAndStoreTokens(HttpContext);

            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
                new CookieOptions
                {
                    HttpOnly = false, // Bắt buộc false để Angular / Cilent có thể đọc Cookie XSRF-TOKEN
                    Secure = true,    // Chạy trên môi trường remote cần flag Secure = true
                    SameSite = SameSiteMode.None // SameSite None vì FE angular 4200 khác port với BE
                });

            return Ok(new { message = "Antiforgery token generated" });
        }
    }
}
