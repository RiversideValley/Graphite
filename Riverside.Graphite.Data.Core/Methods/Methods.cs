using Microsoft.EntityFrameworkCore;
using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Riverside.Graphite.Data.Core.Methods
{
	public class Methods
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

		public static async Task<IEnumerable<string>> GetPendingMigrations(DbContext context)
		{
			var appliedMigrations = await context?.Database?.GetAppliedMigrationsAsync();
			var allMigrations = context?.Database?.GetMigrations();
			return allMigrations.Except(appliedMigrations);
		}

		public static async Task<bool> ApplyPendingMigrations(DbContext context)
		{
			var pendingMigrations = await GetPendingMigrations(context);
			if (pendingMigrations.Any())
			{
				foreach (var migration in pendingMigrations)
				{
					try
					{
						await context?.Database?.MigrateAsync(migration);
					}
					catch (Exception e)
					{
						ExceptionLogger.LogException(e);
						return false;
					}
				}
				return true;
			}
			return false;
		}
	}

}

