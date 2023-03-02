﻿using SantiyeOnMuh.WebUI.Models.Modeller;
using System.ComponentModel.DataAnnotations;

namespace SantiyeOnMuh.WebUI.Models.Modeller
{
    public class Nakit
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "TARİH GİRMELİSİNİZ")]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime Tarih { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
        public string ImgUrl { get; set; }
        public int? BankaKasaKaynak { get; set; }
        public int? CariKasaKaynak { get; set; }
        public DateTime SistemeGiris { get; set; }
        public DateTime SonGuncelleme { get; set; }
        public bool Durum { get; set; }
        public int CariHesapId { get; set; }
        public CariHesap CariHesap { get; set; }
        public int SirketId { get; set; }
        public Sirket Sirket { get; set; }
        public int BankaHesapId { get; set; }
        public BankaHesap BankaHesap { get; set; }
        public Nakit()
        {
            Tarih = System.DateTime.Now;

            BankaKasaKaynak = null;
            CariKasaKaynak = null;

            Durum = true;

            SistemeGiris = System.DateTime.Now;
            SonGuncelleme = System.DateTime.Now;
        }
    }
}
