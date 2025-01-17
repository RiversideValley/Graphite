using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Riverside.Graphite.Data.Core.Actions
{
    public static class Methods
    {
        public static int GetCountFromSqlInterpolated(FormattableString sql, DbContext context)
        {
            var count = context.Database.ExecuteSqlInterpolated(sql);
            return count;
        }

        // Method to execute the SQL query and get the result
        public static List<T> GetEntitiesFromSql<T>(string sql, DbContext context) where T : class
        {
            var entities = context.Set<T>().FromSqlRaw(sql).ToList();
            return entities;
        }
    }

}

