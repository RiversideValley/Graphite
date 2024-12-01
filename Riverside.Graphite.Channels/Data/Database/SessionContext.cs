using FireCore.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace FireCore.Data.Database
{
    //https://github.com/karenpayneoregon/ef-code-8-samples/blob/master/BooksApp/Data/BookContext.cs
    //https://learn.microsoft.com/en-us/ef/core/cli/dotnet
    //https://stackoverflow.com/questions/43709657/how-to-get-root-directory-of-project-in-asp-net-core-directory-getcurrentdirect
    public class SessionContext : DbContext
    {
        public virtual DbSet<SessionDbEntity> SessionDb { get; set; }
        public string? ConnectionString { get; set; }
        public SessionContext(IWebHostEnvironment webHostEnvironment)
        {
            ConnectionString = "Sessions.db";
        }
        public SessionContext(DbContextOptions<SessionContext> options)
       : base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={ConnectionString}");
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SessionDbEntity>(model =>
            {
                model.Property(e => e.DateTime).HasDefaultValue(DateTimeOffset.Now);
                modelBuilder.Entity<SessionDbEntity>();

            });

            base.OnModelCreating(modelBuilder);
        }

    }


}
