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
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.KullaniciAd) || string.IsNullOrEmpty(loginRequest.Parola))
            {
                return BadRequest(new { message = "Kullanıcı adı veya parola boş olamaz" });
            }

            var personel = _context.Personellers
                            .FirstOrDefault(p => p.PersonelKullaniciAd == loginRequest.KullaniciAd && p.PersonelParola == loginRequest.Parola);

            if (personel != null) // personel null değilse giriş yapabilir, nullsa giremez
            {
                HttpContext.Session.SetString("PersonelAdSoyad", personel.PersonelAdSoyad ?? string.Empty);
                HttpContext.Session.SetInt32("PersonelId", personel.PersonelId);
                HttpContext.Session.SetInt32("PersonelBirimId", personel.PersonlBirimId ?? 0);
                HttpContext.Session.SetInt32("PersonelYetkiTurID", personel.PersonelYetkiTurId ?? 0);

                var response = new
                {
                    PersonelAdSoyad = personel.PersonelAdSoyad,
                    PersonelId = personel.PersonelId,
                    PersonelBirimId = personel.PersonlBirimId ?? 0,
                    PersonelYetkiTurId = personel.PersonelYetkiTurId ?? 0
                };

                // Kullanıcının yetki türüne göre yönlendirme
                if (personel.PersonelYetkiTurId == 1) // 1: Yönetici yetki türü
                {
                    // Yöneticiyi yönlendirmek için
                    return Ok(new { redirectUrl = "/Yonetici/Index" }); // Yönetici sayfasına yönlendirme
                }
                else if (personel.PersonelYetkiTurId == 2) // 2: Çalışan yetki türü
                {
                    // Çalışanı yönlendirmek için
                    return Ok(new { redirectUrl = "/Calisan/Index" }); // Çalışan sayfasına yönlendirme
                }
                else
                {
                    return Unauthorized(new { message = "Geçersiz yetki." });
                }
            }
            else
            {
                return Unauthorized(new { message = "Kullanıcı adı veya parola yanlış" });
            }
        }
    }

    public class LoginRequestModel
    {
        public string? KullaniciAd { get; set; }
        public string? Parola { get; set; }
    }
}
