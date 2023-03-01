﻿using SantiyeOnMuh.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SantiyeOnMuh.Business.Abstract
{
    public interface IBankaKasaService : IService<BankaKasa>
    {
        BankaKasa GetByIdDetay(int id);
        List<BankaKasa> GetAll(int? bankahesapid, bool drm);
        List<BankaKasa> GetAll(int? bankahesapid, bool drm, int page, int pageSize);
        int GetCount(int? bankahesapid, bool drm);
    }
}
