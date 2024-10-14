using Microsoft.AspNetCore.Mvc;
using CalisanTakip.Models;
using CalisanTakip.Repository;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace CalisanTakip.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IsTakipDbContext _context;

        public LoginController(IsTakipDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestModel loginRequest)
        {
            // Giriş bilgilerinin kontrolü
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.KullaniciAd) || string.IsNullOrEmpty(loginRequest.Parola))
            {
                return BadRequest(new { message = "Kullanıcı adı veya parola boş olamaz" });
            }

            // Kullanıcıyı veritabanında bulma
            var personel = _context.Personellers
                            .FirstOrDefault(p => p.PersonelKullaniciAd == loginRequest.KullaniciAd && p.PersonelParola == loginRequest.Parola);

            // Kullanıcı bulundu mu?
            if (personel != null)
            {
                // Session bilgilerini ayarlama
                HttpContext.Session.SetString("PersonelAdSoyad", personel.PersonelAdSoyad ?? string.Empty);
                HttpContext.Session.SetInt32("PersonelId", personel.PersonelId);
                HttpContext.Session.SetInt32("PersonelBirimId", personel.PersonlBirimId ?? 0);
                HttpContext.Session.SetInt32("PersonelYetkiTurID", personel.PersonelYetkiTurId ?? 0);

                // Yanıt nesnesi
                var response = new
                {
                    PersonelAdSoyad = personel.PersonelAdSoyad,
                    PersonelId = personel.PersonelId,
                    PersonelBirimId = personel.PersonlBirimId ?? 0,
                    PersonelYetkiTurId = personel.PersonelYetkiTurId ?? 0,
                    RedirectUrl = personel.PersonelYetkiTurId == 1 ? "/Yonetici/Index" : "/calisan/index" // Kullanıcının yetki türüne göre yönlendirme
                };

                return Ok(response); // Başarılı giriş yanıtı
            }
            else
            {
                return Unauthorized(new { message = "Kullanıcı adı veya parola yanlış" }); // Yanlış giriş
            }
        }
    }

    // Giriş isteği model sınıfı
    public class LoginRequestModel
    {
        public string? KullaniciAd { get; set; }
        public string? Parola { get; set; }
    }
}
