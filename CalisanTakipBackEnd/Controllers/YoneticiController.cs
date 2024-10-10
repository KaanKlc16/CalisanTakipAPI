using CalisanTakip.Models;
using CalisanTakip.Repository;
using CalisanTakip.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CalisanTakip.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YoneticiController : ControllerBase
    {
        private readonly IsTakipDbContext _context;

        public YoneticiController()
        {
            _context = new IsTakipDbContext();
        }

        // Yönetici ana sayfası - yetkilendirme kontrolü ve bilgiler
        [HttpGet("index")]
        public IActionResult Index()
        {
            var personelId = HttpContext.Session.GetInt32("PersonelId");
            var personelYetkiTurID = HttpContext.Session.GetInt32("PersonelYetkiTurID");

            if (personelYetkiTurID == 1)
            {
                var birimId = HttpContext.Session.GetInt32("PersonelBirimId");
                var birimAd = _context.Birimlers
                    .Where(b => b.BirimId == birimId)
                    .Select(b => b.BirimAd)
                    .FirstOrDefault();

                return Ok(new
                {
                    PersonelId = personelId,
                    PersonelYetkiTurID = personelYetkiTurID,
                    BirimAd = birimAd
                });
            }
            else
            {
                return Unauthorized("Yetkisiz erişim.");
            }
        }

        // Görev atama sayfası
        [HttpGet("ata")]
        public IActionResult Ata()
        {
            var personelYetkiTurID = HttpContext.Session.GetInt32("PersonelYetkiTurID");

            if (personelYetkiTurID == 1)
            {
                var birimId = HttpContext.Session.GetInt32("PersonelBirimId");

                var calisanlar = _context.Personellers
                    .Where(p => p.PersonlBirimId == birimId && p.PersonelYetkiTurId == 2)
                    .ToList();

                var birimAd = _context.Birimlers
                    .Where(b => b.BirimId == birimId)
                    .Select(b => b.BirimAd)
                    .FirstOrDefault();

                return Ok(new
                {
                    Personeller = calisanlar,
                    BirimAd = birimAd
                });
            }
            else
            {
                return Unauthorized("Yetkisiz erişim.");
            }
        }

        // Görev atama işlemi
        [HttpPost("ata")]
        public IActionResult Ata([FromBody] Isler model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            model.IsOkunma = false;
            model.IletilenTarih = DateTime.Now;
            model.IsDurumId = 1;

            _context.Islers.Add(model);
            _context.SaveChanges();

            return Ok(new { success = true, message = "Görev başarıyla atandı." });
        }

        // Görev takip sayfası
        [HttpGet("takip")]
        public IActionResult Takip()
        {
            var personelYetkiTurID = HttpContext.Session.GetInt32("PersonelYetkiTurID");

            if (personelYetkiTurID == 1)
            {
                var birimId = HttpContext.Session.GetInt32("PersonelBirimId");
                var calisanlar = _context.Personellers
                    .Where(p => p.PersonlBirimId == birimId && p.PersonelYetkiTurId == 2)
                    .ToList();

                var birimAd = _context.Birimlers
                    .Where(b => b.BirimId == birimId)
                    .Select(b => b.BirimAd)
                    .FirstOrDefault();

                return Ok(new
                {
                    Personeller = calisanlar,
                    BirimAd = birimAd
                });
            }
            else
            {
                return Unauthorized("Yetkisiz erişim.");
            }
        }

        // Takip edilen personelin görevleri
        [HttpPost("takip")]
        public IActionResult Takip([FromBody] int selectPer)
        {
            var secilenPersonel = _context.Personellers
                .FirstOrDefault(p => p.PersonelId == selectPer);

            if (secilenPersonel == null)
            {
                return NotFound("Seçilen personel bulunamadı.");
            }

            HttpContext.Session.SetString("SecilenPersonel", JsonConvert.SerializeObject(secilenPersonel));

            return Ok(new { success = true, message = "Personel seçildi.", secilenPersonel });
        }

        // Seçilen personelin görevlerinin listelenmesi
        [HttpGet("listele")]
        public IActionResult Listele()
        {
            var personelYetkiTurID = HttpContext.Session.GetInt32("PersonelYetkiTurID");
            var secilenPersonelJson = HttpContext.Session.GetString("SecilenPersonel");

            if (string.IsNullOrEmpty(secilenPersonelJson))
            {
                return BadRequest("Seçilen personel bulunamadı.");
            }

            var secilenPersonel = JsonConvert.DeserializeObject<Personeller>(secilenPersonelJson);

            if (personelYetkiTurID == 1)
            {
                try
                {
                    var isler = _context.Islers
                        .Where(i => i.IsPersonelId == secilenPersonel.PersonelId)
                        .OrderByDescending(i => i.IletilenTarih)
                        .ToList();

                    return Ok(new
                    {
                        Isler = isler,
                        SecilenPersonel = secilenPersonel,
                        IsSayisi = isler.Count
                    });
                }
                catch (Exception)
                {
                    return StatusCode(500, "Görev listelenirken bir hata oluştu.");
                }
            }
            else
            {
                return Unauthorized("Yetkisiz erişim.");
            }
        }

        // Takvimdeki görevleri getirme
        [HttpGet("getCalendarEvents")]
        public IActionResult GetCalendarEvents()
        {
            var personelBirimId = HttpContext.Session.GetInt32("PersonelBirimId");

            if (personelBirimId == null)
            {
                return BadRequest("Personel Birim ID bulunamadı.");
            }

            var isDurumlar = _context.Islers
                .Include(i => i.IsPersonel)
                .Include(i => i.IsDurum)
                .Where(i => i.IsPersonel.PersonlBirimId == personelBirimId)
                .Select(i => new
                {
                    i.IsId,
                    i.IsBaslik,
                    i.IsAciklama,
                    i.IsBaslangic,
                    i.IsBitirmeSure,
                    personelAdSoyad = i.IsPersonel.PersonelAdSoyad,
                    i.TahminiSure
                })
                .ToList();

            var events = isDurumlar.Select(d => new
            {
                id = d.IsId,
                title = $"{d.IsBaslik} - {d.personelAdSoyad}",
                start = d.IsBaslangic.HasValue ? d.IsBaslangic.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                end = d.IsBitirmeSure.HasValue ? d.IsBitirmeSure.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                description = d.IsAciklama,
                tahminiSure = $"{d.TahminiSure} saat"
            });

            return Ok(events);
        }

        // Takvimdeki görev güncelleme
        [HttpPost("updateEvent")]
        public IActionResult UpdateEvent([FromBody] TakvimGuncelle model)
        {
            if (model == null)
            {
                return BadRequest("Model verisi alınamadı.");
            }

            var isler = _context.Islers.FirstOrDefault(i => i.IsId == model.Id);

            if (isler == null)
            {
                return NotFound("Görev bulunamadı.");
            }

            if (model.Start != DateTime.MinValue)
            {
                isler.IsBaslangic = model.Start;
            }

            if (model.End != DateTime.MinValue)
            {
                isler.IsBitirmeSure = model.End;
            }

            _context.SaveChanges();

            return Ok(new { success = true, message = "Görev başarıyla güncellendi." });
        }
    }
}
