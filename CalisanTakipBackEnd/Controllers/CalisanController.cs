using CalisanTakip.Repository;
using CalisanTakip.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CalisanTakip.Controllers
{ //İPTAL
    [ApiController]
    [Route("api/[controller]")]
    public class CalisanController : ControllerBase
    {
        private readonly IsTakipDbContext _context;

        public CalisanController()
        {
            _context = new IsTakipDbContext();
        }

        // GET: api/calisan
        [HttpGet]
        public IActionResult GetPersonelData()
        {
            var personelAdSoyad = HttpContext.Session.GetString("PersonelAdSoyad");
            var personelId = HttpContext.Session.GetInt32("PersonelId");
            var personelBirimId = HttpContext.Session.GetInt32("PersonelBirimId");
            var personelYetkiTurID = HttpContext.Session.GetInt32("PersonelYetkiTurID");

            var birimAd = _context.Birimlers
                .Where(b => b.BirimId == personelBirimId)
                .Select(b => b.BirimAd)
                .FirstOrDefault();

            if (personelYetkiTurID == 2)
            {
                var isler = _context.Islers
                    .Where(i => i.IsPersonelId == personelId && i.IsOkunma == false)
                    .OrderByDescending(i => i.IletilenTarih)
                    .ToList();

                var response = new
                {
                    PersonelAdSoyad = personelAdSoyad,
                    PersonelId = personelId,
                    PersonelBirimId = personelBirimId,
                    PersonelYetkiTurID = personelYetkiTurID,
                    BirimAd = birimAd,
                    Isler = isler
                };

                return Ok(response);  
            }
            else
            {
                return Forbid();
            }
        }

        // POST: api/calisan
        [HttpPost]
        public IActionResult MarkIsAsRead([FromBody] int isId)
        {
            var tekIs = _context.Islers.FirstOrDefault(i => i.IsId == isId);

            if (tekIs != null)
            {
                tekIs.IsOkunma = true;
                _context.SaveChanges();
                return Ok();
            }

            return NotFound();
        }

        // GET: api/calisan/yap
        [HttpGet("yap")]
        public IActionResult GetYapilacakIsler()
        {
            var personelYetkiTurID = HttpContext.Session.GetInt32("PersonelYetkiTurID");

            if (personelYetkiTurID == 2)
            {
                var personelId = HttpContext.Session.GetInt32("PersonelId");
                var isler = _context.Islers
                    .Where(i => i.IsPersonelId == personelId && i.IsDurumId == 1)
                    .OrderByDescending(i => i.IletilenTarih)
                    .ToList();

                return Ok(isler);  // JSON formatında döndür
            }
            else
            {
                return Forbid();
            }
        }

        // POST: api/calisan/yap
        [HttpPost("yap")]
        public IActionResult UpdateIsYapilacak([FromBody] YapIsModel model)
        {
            var tekIs = _context.Islers.FirstOrDefault(i => i.IsId == model.IsId);

            if (tekIs != null)
            {
                tekIs.YapilanTarih = DateTime.Now;
                tekIs.IsDurumId = 2;

                TimeSpan tahminiSure = new TimeSpan(model.TahminiSureSaat, model.TahminiSureDakika, 0);
                tekIs.TahminiSure = (int)tahminiSure.TotalMinutes;
                tekIs.IsYorum = string.IsNullOrEmpty(model.IsYorum) ? "Çalışan Yorum Yapmadı" : model.IsYorum;

                _context.SaveChanges();
                return Ok();
            }

            return NotFound();
        }

        // GET: api/calisan/takip
        [HttpGet("takip")]
        public IActionResult GetTakip()
        {
            var personelYetkiTurID = HttpContext.Session.GetInt32("PersonelYetkiTurID");

            if (personelYetkiTurID == 2)
            {
                var personelId = HttpContext.Session.GetInt32("PersonelId");

                var isler = (from i in _context.Islers
                             join d in _context.Durumlars on i.IsDurumId equals d.DurumId
                             where i.IsPersonelId == personelId
                             orderby i.IletilenTarih descending
                             select new
                             {
                                 i.IsBaslik,
                                 i.IsAciklama,
                                 i.IletilenTarih,
                                 i.YapilanTarih,
                                 d.DurumAd,
                                 i.IsYorum
                             }).ToList();

                return Ok(isler);  // JSON formatında döndür
            }
            else
            {
                return Forbid();
            }
        }

        // GET: api/calisan/calendar
        [HttpGet("calendar")]
        public IActionResult GetCalendarEvents()
        {
            var birimId = HttpContext.Session.GetInt32("PersonelBirimId");

            var isDurumlar = _context.Islers
                .Include(i => i.IsPersonel)
                .Include(i => i.IsDurum)
                .Where(i => i.IsPersonel.PersonlBirimId == birimId)
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
                calendarId = "1",
                title = d.IsBaslik + " - " + d.personelAdSoyad,
                category = "time",
                start = d.IsBaslangic?.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = d.IsBitirmeSure?.ToString("yyyy-MM-ddTHH:mm:ss"),
                description = d.IsAciklama,
                personelAdSoyad = d.personelAdSoyad,
                tahminiSure = d.TahminiSure != null ? $"{d.TahminiSure} saat" : "Tahmini süre girilmedi"
            });

            return Ok(events);  // JSON formatında döndür
        }

        // POST: api/calisan/updatetahminisure
        [HttpPost("updatetahminisure")]
        public IActionResult UpdateTahminiSure([FromBody] UpdateTahminiSureModel model)
        {
            var isler = _context.Islers.FirstOrDefault(i => i.IsId == model.IsId);
            if (isler != null)
            {
                isler.TahminiSure = model.TahminiSure;
                _context.SaveChanges();
                return Ok();
            }
            return NotFound();
        }
    }

    
    public class YapIsModel
    {
        public int IsId { get; set; }
        public string? IsYorum { get; set; }
        public int TahminiSureSaat { get; set; }
        public int TahminiSureDakika { get; set; }
    }

    public class UpdateTahminiSureModel
    {
        public int IsId { get; set; }
        public int TahminiSure { get; set; }
    }
}
