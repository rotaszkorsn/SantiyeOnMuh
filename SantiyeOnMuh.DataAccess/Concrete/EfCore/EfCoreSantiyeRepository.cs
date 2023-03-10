using SantiyeOnMuh.DataAccess.Abstract;
using SantiyeOnMuh.DataAccess.Concrete.EfCore;
using SantiyeOnMuh.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SantiyeOnMuh.DataAccess.Concrete.SqlServer
{
    public class EfCoreSantiyeRepository : EfCoreGenericRepository<ESantiye, Context>, ISantiyeRepository
    {
        public List<ESantiye> GetAll(bool drm)
        {
            using (var context = new Context())
            {
                return context.Santiyes
                    .Where(i => i.Durum == drm)
                    .ToList();
            }
        }
    }
}
