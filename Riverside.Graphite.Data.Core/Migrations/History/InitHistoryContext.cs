using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Riverside.Graphite.Data.Core;

#nullable disable

namespace Riverside.Graphite.Data.Core.Migrations.History
{
    [DbContext(typeof(HistoryContext))]
    partial class InitHistoryContext : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.1");

            modelBuilder.Entity("Riverside.Graphite.Data.Core.Models.DbHistoryItem", b =>
            {
                b.Property<int>("id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<int>("hidden")
                    .HasColumnType("INTEGER");

                b.Property<string>("last_visit_time")
                    .HasColumnType("TEXT");

                b.Property<string>("title")
                    .HasColumnType("TEXT");

                b.Property<int>("typed_count")
                    .HasColumnType("INTEGER");

                b.Property<string>("url")
                    .HasColumnType("TEXT");

                b.Property<int>("visit_count")
                    .HasColumnType("INTEGER");

                b.HasKey("id");

                b.ToTable("Urls");
            });
#pragma warning restore 612, 618
        }
    }
}
