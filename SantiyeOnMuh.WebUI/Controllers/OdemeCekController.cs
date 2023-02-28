﻿using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SantiyeOnMuh.Business.Abstract;
using SantiyeOnMuh.Entity;
using SantiyeOnMuh.WebUI.Models;
using System.Runtime.ConstrainedExecution;

namespace SantiyeOnMuh.WebUI.Controllers
{
    public class OdemeCekController : Controller
    {
        // NESNELER ÜZERİNDEKİ İŞLEMLERİ _ OLAN NESNE ÜZERİNDE YAPIP SONRA AKTARIYORUZ - INJECTION
        private IOdemeCekService _odemeCekService;
        private ISirketService _sirketService;
        private ICariHesapService _cariHesapService;
        private IBankaHesapService _bankaHesapService;
        private IBankaKasaService _bankaKasaService;
        private ICariKasaService _cariKasaService;
        private ISantiyeService _santiyeService;
        public OdemeCekController(
            IOdemeCekService odemeCekService,
            ISirketService sirketService,
            ICariHesapService cariHesapService,
            IBankaHesapService bankaHesapService,
            IBankaKasaService bankaKasaService,
            ICariKasaService cariKasaService,
            ISantiyeService santiyeService)
        {
            this._odemeCekService = odemeCekService;
            this._sirketService = sirketService;
            this._cariHesapService = cariHesapService;
            this._bankaHesapService = bankaHesapService;
            this._bankaKasaService = bankaKasaService;
            this._cariKasaService = cariKasaService;
            this._santiyeService = santiyeService;
        }
        public IActionResult Index(int page = 1)
        {
            ViewBag.Sayfa = "ÇEKLER";
            const int pageSize = 10;
            var odemeCekViewModel = new OdemeCekViewListModel()
            {
                PageInfo = new PageInfo
                {
                    TotalItem = _odemeCekService.GetCount(null, null, null, true),
                    CurrentPage = page,
                    ItemPerPage = pageSize,
                    UrlInfo = null
                },
                OdemeCeks = _odemeCekService.GetAll(null, null, null, true, page, pageSize),
            };
            return View(odemeCekViewModel);
        }
        [HttpGet]
        public IActionResult CekEkleme()
        {
            ViewBag.Sayfa = "YENİ ÇEK EKLEME";
            ViewBag.Sirket = _sirketService.GetAll(true);
            ViewBag.Cari = _cariHesapService.GetAll(null, true);
            ViewBag.Banka = _bankaHesapService.GetAll(true);

            return View(new OdemeCek());
        }
        [HttpPost]
        public async Task<IActionResult> CekEkleme(OdemeCek c, IFormFile file)
        {
            #region RESİM VS. EKLENMEMİŞSE SAYFAYA GERİ GİDİYOR, GERİ GİDİLEN SAYFANIN İHTİYACI OLAN BİLGİLER
            ViewBag.Sayfa = "YENİ ÇEK EKLEME";
            ViewBag.Sirket = _sirketService.GetAll(true);
            ViewBag.Cari = _cariHesapService.GetAll(null, true);
            ViewBag.Banka = _bankaHesapService.GetAll(true);
            #endregion
            #region RESİM EKLEME BÖLÜMÜ
            if (file != null)
            {
                var extension = Path.GetExtension(file.FileName);

                if (extension == ".jpg" || extension == ".png" || extension == ".pdf")
                {
                    var cekName = string.Format($"{c.CekNo}{"-"}{c.Aciklama}{"-"}{Guid.NewGuid()}{extension}");
                    c.ImgUrl = cekName;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\CekResim", cekName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                else { return View(c); }
            }
            else { return View(c); }
            #endregion
            _odemeCekService.Create(c);
            //CARİ KASA İÇİN ÇEK OLUŞTURULDU VE ÇEK KAYNAĞI İLE EKLENDİ
            CariKasa EntityCariKasa = new CariKasa()
            {
                Tarih = c.Tarih,
                Aciklama = c.CekNo + " NOLU CEK ÖDEMESİ " + c.Aciklama,
                Miktar = 1,
                BirimFiyat = 1,
                Borc = c.Tutar,
                Alacak = 0,
                ImgUrl = null,
                CekKaynak = c.Id,
                CariGiderKalemiId = 1,
                CariHesapId = c.CariHesapId
            };
            _cariKasaService.Create(EntityCariKasa);
            //ŞİMDİ DE ÇEKE, CARİ KAYNAĞI EKLENİYOR-GÜNCELLENİYOR
            var EntityEklenenCek = _odemeCekService.GetById(c.Id);
            if (EntityEklenenCek == null)
            {
                return NotFound();
            }
            EntityEklenenCek.CariKasaKaynak = EntityCariKasa.Id;
            _odemeCekService.Update(EntityEklenenCek);

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult CekTahsil(int? cekid)
        {
            ViewBag.Sayfa = "ÇEK TAHSİL";

            if (cekid == null)
            {
                return NotFound();
            }

            OdemeCek c = _odemeCekService.GetById((int)cekid);

            if (c == null)
            {
                return NotFound();
            }

            var entity = _odemeCekService.GetById(c.Id);

            if (entity == null)
            {
                return NotFound();
            }

            //CARİ KASA İÇİN ÇEK OLUŞTURULDU VE ÇEK KAYNAĞI İLE EKLENDİ
            String FirmaAdiForAciklama = _cariHesapService.GetById((int)c.CariHesapId).Ad;

            BankaKasa EntityBankaKasa = new BankaKasa()
            {
                Tarih = entity.Tarih,
                Aciklama = FirmaAdiForAciklama + " AİT " + entity.CekNo + " NOLU ÇEK ÖDEMESİ. " + " - " + entity.Aciklama,
                Nitelik = "ÇEK",
                Cikan = entity.Tutar,
                Giren = 0,
                CekKaynak = entity.Id,
                BankaHesapId = entity.BankaHesapId,
                Durum = true
            };

            _bankaKasaService.Create(EntityBankaKasa);

            entity.Durum = true;
            entity.OdemeDurumu = true;
            entity.SonGuncelleme = DateTime.Now;
            entity.BankaKasaKaynak = EntityBankaKasa.Id;

            _odemeCekService.Update(entity);

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult CekGuncelle(int? cekid)
        {
            ViewBag.Sayfa = "ÇEK BİLGİLERİNİ GÜNCELLEME";
            ViewBag.Sirket = _sirketService.GetAll(true);
            ViewBag.Cari = _cariHesapService.GetAll(null, true);
            ViewBag.Banka = _bankaHesapService.GetAll(true);

            if (cekid == null)
            {
                return NotFound();
            }
            OdemeCek cek = _odemeCekService.GetById((int)cekid);

            if (cek == null)
            {
                return NotFound();
            }
            return View(cek);
        }
        [HttpPost]
        public IActionResult CekGuncelle(OdemeCek c)
        {
            var entityOdemeCek = _odemeCekService.GetById(c.Id);

            if (entityOdemeCek == null)
            {
                return NotFound();
            }

            int? bankakasaid = entityOdemeCek.BankaKasaKaynak;
            int? carikasaid = entityOdemeCek.CariKasaKaynak;

            if (bankakasaid != null)
            {
                String FirmaAdiForAciklama = _cariHesapService.GetById((int)c.CariHesapId).Ad;
                var entityBankaKasa = _bankaKasaService.GetById((int)bankakasaid);
                if (entityBankaKasa == null)
                {
                    return NotFound();
                }

                entityBankaKasa.Tarih = entityOdemeCek.Tarih;
                entityBankaKasa.Aciklama = FirmaAdiForAciklama + " AİT " + entityOdemeCek.CekNo + " NOLU ÇEK ÖDEMESİ" + " - " + entityOdemeCek.Aciklama;
                entityBankaKasa.Cikan = entityOdemeCek.Tutar;
                entityBankaKasa.BankaHesapId = entityOdemeCek.BankaHesapId;
                entityBankaKasa.SonGuncelleme = DateTime.Now;

                _bankaKasaService.Update(entityBankaKasa);
            }

            if (carikasaid != null)
            {
                var entityCariKasa = _cariKasaService.GetById((int)carikasaid);
                if (entityCariKasa == null)
                {
                    return NotFound();
                }

                entityCariKasa.Tarih = c.Tarih;
                entityCariKasa.Aciklama = c.CekNo + " NOLU CEK ÖDEMESİ" + " - " + c.Aciklama;
                entityCariKasa.Borc = c.Tutar;
                entityCariKasa.SonGuncelleme = DateTime.Now;
                entityCariKasa.CariHesapId = c.CariHesapId;

                _cariKasaService.Update(entityCariKasa);
            }

            entityOdemeCek.Tarih = c.Tarih;
            entityOdemeCek.Aciklama = c.Aciklama;
            entityOdemeCek.CekNo = c.CekNo;
            entityOdemeCek.Tutar = c.Tutar;
            entityOdemeCek.ImgUrl = c.ImgUrl;
            entityOdemeCek.SonGuncelleme = System.DateTime.Now;
            entityOdemeCek.CariHesapId = c.CariHesapId;
            entityOdemeCek.SirketId = c.SirketId;
            entityOdemeCek.BankaHesapId = c.BankaHesapId;

            _odemeCekService.Update(entityOdemeCek);

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Cek(int? cekid)
        {
            ViewBag.Sayfa = "ÇEK DETAYI";

            if (cekid == null)
            {
                return NotFound();
            }

            OdemeCek cek = _odemeCekService.GetById((int)cekid);

            if (cek == null)
            {
                return NotFound();
            }

            return View(cek);
        }
        [HttpGet]
        public IActionResult CekSil(int? cekid)
        {
            ViewBag.Sayfa = "ÇEKİ SİL";

            if (cekid == null)
            {
                return NotFound();
            }
            OdemeCek cek = _odemeCekService.GetById((int)cekid);

            if (cek == null)
            {
                return NotFound();
            }
            return View(cek);
        }
        [HttpPost]
        public IActionResult CekSil(OdemeCek c)
        {
            var entityCek = _odemeCekService.GetById(c.Id);

            if (entityCek == null)
            {
                return NotFound();
            }

            entityCek.SonGuncelleme = System.DateTime.Now;
            entityCek.Durum = false;

            int? bankakasaid = entityCek.BankaKasaKaynak;
            int? carikasaid = entityCek.CariKasaKaynak;

            if (bankakasaid != null)
            {
                var entityBankaKasa = _bankaKasaService.GetById((int)bankakasaid);

                if (entityBankaKasa == null)
                {
                    return NotFound();
                }

                entityBankaKasa.SonGuncelleme = System.DateTime.Now;
                entityBankaKasa.Durum = false;

                _bankaKasaService.Update(entityBankaKasa);
            }

            if (carikasaid != null)
            {
                var entityCariKasa = _cariKasaService.GetById((int)carikasaid);

                if (entityCariKasa == null)
                {
                    return NotFound();
                }

                entityCariKasa.SonGuncelleme = System.DateTime.Now;
                entityCariKasa.Durum = false;

                _cariKasaService.Update(entityCariKasa);
            }

            _odemeCekService.Update(entityCek);

            return RedirectToAction("Index");
        }


        //EXCEL
        public IActionResult CekExcel()
        {
            var cekViewModel = new OdemeCekViewListModel()
            {
                OdemeCeks = _odemeCekService.GetAll(null, null, null, true),
            };

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("ÇEK LİSTESİ");
                var currentRow = 1;
                decimal toplamodenmis = 0;
                decimal toplamodenmemis = 0;
                decimal toplam = 0;

                #region Header
                worksheet.Cell(currentRow, 1).Value = "TARİH";
                worksheet.Cell(currentRow, 2).Value = "ÇEK NO";
                worksheet.Cell(currentRow, 3).Value = "AÇIKLAMA";
                worksheet.Cell(currentRow, 4).Value = "İŞLENEN ÇEK";
                worksheet.Cell(currentRow, 5).Value = "ÖDENEN ÇEK";
                worksheet.Cell(currentRow, 6).Value = "TOPLAM";

                for (int i = 1; i < 7; i++)
                {
                    worksheet.Cell(currentRow, i).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                #endregion

                #region Body
                foreach (var cek in cekViewModel.OdemeCeks)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = cek.Tarih;
                    worksheet.Cell(currentRow, 2).Value = cek.CekNo;
                    worksheet.Cell(currentRow, 3).Value = cek.Aciklama;

                    if (cek.OdemeDurumu == false)
                    {
                        worksheet.Cell(currentRow, 4).Value = cek.Tutar;
                        toplamodenmemis = toplamodenmemis + cek.Tutar;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 5).Value = cek.Tutar;
                        toplamodenmis = toplamodenmis + cek.Tutar;
                    }

                    toplam = toplam + cek.Tutar;
                    worksheet.Cell(currentRow, 6).Value = toplam;

                    for (int i = 1; i < 7; i++)
                    {
                        worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                }
                #endregion

                #region FOOTER
                worksheet.Cell(currentRow + 2, 4).Value = toplamodenmemis;
                worksheet.Cell(currentRow + 2, 5).Value = toplamodenmis;
                worksheet.Cell(currentRow + 2, 6).Value = toplam;
                for (int i = 4; i < 7; i++)
                {
                    worksheet.Cell(currentRow + 2, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow + 2, i).Style.Font.Bold = true;
                }

                #endregion

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "CekListesi.xlsx"
                        );
                }
            }
        }
        //ARŞİV
        public IActionResult CekArsiv(int page = 1)
        {
            ViewBag.Sayfa = "SİLİNMİŞ ÇEKLER";
            const int pageSize = 10;
            var cekViewModel = new OdemeCekViewListModel()
            {
                PageInfo = new PageInfo
                {
                    TotalItem = _odemeCekService.GetCount(null, null, null, false),
                    CurrentPage = page,
                    ItemPerPage = pageSize,
                    UrlInfo = null
                },
                OdemeCeks = _odemeCekService.GetAll(null, null, null, false, page, pageSize),
            };
            return View(cekViewModel);
        }

        #region CARİ ÜZERİNDEN
        [HttpGet]
        public IActionResult CekEklemeFromCari(int carihesapid)
        {
            ViewBag.Sayfa = _cariHesapService.GetById((int)carihesapid).Ad + " CARİSİNE YENİ ÇEK GİRİŞİ";

            ViewBag.Sirket = _sirketService.GetAll(true);
            ViewBag.Banka = _bankaHesapService.GetAll(true);

            ViewBag.CariHesapId = carihesapid;

            return View(new OdemeCek());
        }
        [HttpPost]
        public async Task<IActionResult> CekEklemeFromCari(OdemeCek c, IFormFile file)
        {
            #region  RESİM VS. EKLENMEMİŞSE SAYFAYA GERİ GİDİYOR, GERİ GİDİLEN SAYFANIN İHTİYACI OLAN BİLGİLER
            ViewBag.Sayfa = _cariHesapService.GetById((int)c.CariHesapId).Ad + " CARİSİNE YENİ ÇEK GİRİŞİ";

            ViewBag.Sirket = _sirketService.GetAll(true);
            ViewBag.Banka = _bankaHesapService.GetAll(true);

            ViewBag.CariHesapId = c.CariHesapId;
            //ÜSTTEKİ SIFIRDAN EKLEMENİN BİREBİR AYNISI, GEREKLİ BİLGİLER
            #endregion
            #region RESİM EKLEME BÖLÜMÜ
            if (file != null)
            {
                var extension = Path.GetExtension(file.FileName);

                if (extension == ".jpg" || extension == ".png" || extension == ".pdf")
                {
                    var cekName = string.Format($"{c.CekNo}{"-"}{c.Aciklama}{"-"}{Guid.NewGuid()}{extension}");
                    c.ImgUrl = cekName;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\CekResim", cekName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                else { return View(c); }
            }
            else { return View(c); }
            #endregion
            _odemeCekService.Create(c);
            //CARİ KASA İÇİN ÇEK OLUŞTURULDU VE ÇEK KAYNAĞI İLE EKLENDİ
            CariKasa EntityCariKasa = new CariKasa()
            {
                Tarih = c.Tarih,
                Aciklama = c.CekNo + " NOLU CEK ÖDEMESİ " + c.Aciklama,
                Miktar = 1,
                BirimFiyat = 1,
                Borc = c.Tutar,
                Alacak = 0,
                ImgUrl = null,
                CekKaynak = c.Id,
                CariGiderKalemiId = 1,
                CariHesapId = c.CariHesapId
            };
            _cariKasaService.Create(EntityCariKasa);
            //ŞİMDİ DE ÇEKE, CARİ KAYNAĞI EKLENİYOR-GÜNCELLENİYOR
            var EntityEklenenCek = _odemeCekService.GetById(c.Id);
            if (EntityEklenenCek == null)
            {
                return NotFound();
            }
            EntityEklenenCek.CariKasaKaynak = EntityCariKasa.Id;

            _odemeCekService.Update(EntityEklenenCek);

            return RedirectToAction("CariKasa", "CariKasa", new { carihesapid = c.CariHesapId });
        }
        #endregion

        #region BASİT FİLTRE
        public IActionResult AraSecim()
        {
            ViewBag.Sayfa = "ÇEKLER";
            var cekViewModel = new OdemeCekViewListModel()
            {
                Santiyes = _santiyeService.GetAll(true),
                BankaHesaps = _bankaHesapService.GetAll(true),
                Sirkets = _sirketService.GetAll(true),
            };

            return View(cekViewModel);
        }
        public IActionResult CekSantiye(int santiyeid, int page = 1)
        {
            ViewBag.Sayfa = "ÇEKLER";
            const int pageSize = 10;
            var cekViewModel = new OdemeCekViewListModel()
            {
                PageInfo = new PageInfo
                {
                    TotalItem = _odemeCekService.GetCount(santiyeid, null, null, true),
                    CurrentPage = page,
                    ItemPerPage = pageSize,
                    UrlInfo = santiyeid
                },
                OdemeCeks = _odemeCekService.GetAll(santiyeid, null, null, true, page, pageSize),
            };
            return View(cekViewModel);
        }
        public IActionResult CekSirket(int sirketid, int page = 1)
        {
            ViewBag.Sayfa = "ÇEKLER";
            const int pageSize = 10;
            var cekViewModel = new OdemeCekViewListModel()
            {
                PageInfo = new PageInfo
                {
                    TotalItem = _odemeCekService.GetCount(null, sirketid, null, true),
                    CurrentPage = page,
                    ItemPerPage = pageSize,
                    UrlInfo = sirketid
                },
                OdemeCeks = _odemeCekService.GetAll(null, sirketid, null, true, page, pageSize),
            };
            return View(cekViewModel);
        }
        public IActionResult CekBanka(int bankahesapid, int page = 1)
        {
            ViewBag.Sayfa = "ÇEKLER";
            const int pageSize = 10;
            var cekViewModel = new OdemeCekViewListModel()
            {
                PageInfo = new PageInfo
                {
                    TotalItem = _odemeCekService.GetCount(null, null, bankahesapid, true),
                    CurrentPage = page,
                    ItemPerPage = pageSize,
                    UrlInfo = bankahesapid
                },
                OdemeCeks = _odemeCekService.GetAll(null, null, bankahesapid, true, page, pageSize),
            };
            return View(cekViewModel);
        }

        //BASİT FİLTRE EXCEL
        public IActionResult CekSantiyeExcel(int santiyeid)
        {
            var cekViewModel = new OdemeCekViewListModel()
            {
                OdemeCeks = _odemeCekService.GetAll(santiyeid, null, null, true),
            };

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("ÇEK LİSTESİ");
                var currentRow = 1;
                decimal toplamodenmis = 0;
                decimal toplamodenmemis = 0;
                decimal toplam = 0;

                #region Header
                worksheet.Cell(currentRow, 1).Value = "TARİH";
                worksheet.Cell(currentRow, 2).Value = "ÇEK NO";
                worksheet.Cell(currentRow, 3).Value = "AÇIKLAMA";
                worksheet.Cell(currentRow, 4).Value = "İŞLENEN ÇEK";
                worksheet.Cell(currentRow, 5).Value = "ÖDENEN ÇEK";
                worksheet.Cell(currentRow, 6).Value = "TOPLAM";

                for (int i = 1; i < 7; i++)
                {
                    worksheet.Cell(currentRow, i).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                #endregion

                #region Body
                foreach (var cek in cekViewModel.OdemeCeks)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = cek.Tarih;
                    worksheet.Cell(currentRow, 2).Value = cek.CekNo;
                    worksheet.Cell(currentRow, 3).Value = cek.Aciklama;

                    if (cek.OdemeDurumu == false)
                    {
                        worksheet.Cell(currentRow, 4).Value = cek.Tutar;
                        toplamodenmemis = toplamodenmemis + cek.Tutar;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 5).Value = cek.Tutar;
                        toplamodenmis = toplamodenmis + cek.Tutar;
                    }

                    toplam = toplam + cek.Tutar;
                    worksheet.Cell(currentRow, 6).Value = toplam;

                    for (int i = 1; i < 7; i++)
                    {
                        worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                }
                #endregion

                #region FOOTER
                worksheet.Cell(currentRow + 2, 4).Value = toplamodenmemis;
                worksheet.Cell(currentRow + 2, 5).Value = toplamodenmis;
                worksheet.Cell(currentRow + 2, 6).Value = toplam;
                for (int i = 4; i < 7; i++)
                {
                    worksheet.Cell(currentRow + 2, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow + 2, i).Style.Font.Bold = true;
                }

                #endregion

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "CekListesi.xlsx"
                        );
                }
            }
        }
        public IActionResult CekSirketExcel(int sirketid)
        {
            var cekViewModel = new OdemeCekViewListModel()
            {
                OdemeCeks = _odemeCekService.GetAll(null, sirketid, null, true),
            };

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("ÇEK LİSTESİ");
                var currentRow = 1;
                decimal toplamodenmis = 0;
                decimal toplamodenmemis = 0;
                decimal toplam = 0;

                #region Header
                worksheet.Cell(currentRow, 1).Value = "TARİH";
                worksheet.Cell(currentRow, 2).Value = "ÇEK NO";
                worksheet.Cell(currentRow, 3).Value = "AÇIKLAMA";
                worksheet.Cell(currentRow, 4).Value = "İŞLENEN ÇEK";
                worksheet.Cell(currentRow, 5).Value = "ÖDENEN ÇEK";
                worksheet.Cell(currentRow, 6).Value = "TOPLAM";

                for (int i = 1; i < 7; i++)
                {
                    worksheet.Cell(currentRow, i).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                #endregion

                #region Body
                foreach (var cek in cekViewModel.OdemeCeks)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = cek.Tarih;
                    worksheet.Cell(currentRow, 2).Value = cek.CekNo;
                    worksheet.Cell(currentRow, 3).Value = cek.Aciklama;

                    if (cek.OdemeDurumu == false)
                    {
                        worksheet.Cell(currentRow, 4).Value = cek.Tutar;
                        toplamodenmemis = toplamodenmemis + cek.Tutar;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 5).Value = cek.Tutar;
                        toplamodenmis = toplamodenmis + cek.Tutar;
                    }

                    toplam = toplam + cek.Tutar;
                    worksheet.Cell(currentRow, 6).Value = toplam;

                    for (int i = 1; i < 7; i++)
                    {
                        worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                }
                #endregion

                #region FOOTER
                worksheet.Cell(currentRow + 2, 4).Value = toplamodenmemis;
                worksheet.Cell(currentRow + 2, 5).Value = toplamodenmis;
                worksheet.Cell(currentRow + 2, 6).Value = toplam;
                for (int i = 4; i < 7; i++)
                {
                    worksheet.Cell(currentRow + 2, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow + 2, i).Style.Font.Bold = true;
                }

                #endregion

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "CekListesi.xlsx"
                        );
                }
            }
        }
        public IActionResult CekBankaExcel(int bankahesapid)
        {
            var cekViewModel = new OdemeCekViewListModel()
            {
                OdemeCeks = _odemeCekService.GetAll(null, null, bankahesapid, true),
            };

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("ÇEK LİSTESİ");
                var currentRow = 1;
                decimal toplamodenmis = 0;
                decimal toplamodenmemis = 0;
                decimal toplam = 0;

                #region Header
                worksheet.Cell(currentRow, 1).Value = "TARİH";
                worksheet.Cell(currentRow, 2).Value = "ÇEK NO";
                worksheet.Cell(currentRow, 3).Value = "AÇIKLAMA";
                worksheet.Cell(currentRow, 4).Value = "İŞLENEN ÇEK";
                worksheet.Cell(currentRow, 5).Value = "ÖDENEN ÇEK";
                worksheet.Cell(currentRow, 6).Value = "TOPLAM";

                for (int i = 1; i < 7; i++)
                {
                    worksheet.Cell(currentRow, i).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                #endregion

                #region Body
                foreach (var cek in cekViewModel.OdemeCeks)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = cek.Tarih;
                    worksheet.Cell(currentRow, 2).Value = cek.CekNo;
                    worksheet.Cell(currentRow, 3).Value = cek.Aciklama;

                    if (cek.OdemeDurumu == false)
                    {
                        worksheet.Cell(currentRow, 4).Value = cek.Tutar;
                        toplamodenmemis = toplamodenmemis + cek.Tutar;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 5).Value = cek.Tutar;
                        toplamodenmis = toplamodenmis + cek.Tutar;
                    }

                    toplam = toplam + cek.Tutar;
                    worksheet.Cell(currentRow, 6).Value = toplam;

                    for (int i = 1; i < 7; i++)
                    {
                        worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                }
                #endregion

                #region FOOTER
                worksheet.Cell(currentRow + 2, 4).Value = toplamodenmemis;
                worksheet.Cell(currentRow + 2, 5).Value = toplamodenmis;
                worksheet.Cell(currentRow + 2, 6).Value = toplam;
                for (int i = 4; i < 7; i++)
                {
                    worksheet.Cell(currentRow + 2, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(currentRow + 2, i).Style.Font.Bold = true;
                }

                #endregion

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "CekListesi.xlsx"
                        );
                }
            }
        }

        #endregion

    }
}