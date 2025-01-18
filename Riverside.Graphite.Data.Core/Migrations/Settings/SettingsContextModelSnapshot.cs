﻿// <auto-generated />
using Riverside.Graphite.Data.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Riverside.Graphite.Data.Core.Migrations.Settings
{
    [DbContext(typeof(SettingsContext))]
    partial class SettingsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

            modelBuilder.Entity("Riverside.Graphite.Core.Settings", b =>
                {
                    b.Property<string>("PackageName")
                        .HasColumnType("TEXT");

                    b.Property<int>("AdBlockerType")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AdblockBtn")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Auto")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("BackButton")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Background")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("BrowserKeys")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("BrowserScripts")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ColorBackground")
                        .HasColumnType("TEXT");

                    b.Property<string>("ColorTV")
                        .HasColumnType("TEXT");

                    b.Property<string>("ColorTool")
                        .HasColumnType("TEXT");

                    b.Property<bool>("ConfirmCloseDlg")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("DarkIcon")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("DisableGenAutoFill")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("DisableJavaScript")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("DisablePassSave")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("DisableWebMess")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Downloads")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EngineFriendlyName")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Eq2fa")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("EqHis")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Eqfav")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Eqsets")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ExceptionLog")
                        .HasColumnType("TEXT");

                    b.Property<bool>("ExitDialog")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Favorites")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("FavoritesL")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ForwardButton")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Gender")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Historybtn")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("HomeButton")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsAdBlockerEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsFavoritesToggled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsFavoritesVisible")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsHistoryToggled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsHistoryVisible")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsLogoVisible")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSearchBoxToggled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSearchVisible")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsTrendingVisible")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Lang")
                        .HasColumnType("TEXT");

                    b.Property<bool>("LightMode")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("NewTabHistoryDownloads")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("NewTabHistoryFavorites")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("NewTabHistoryHistory")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("NewTabHistoryQuick")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("NtpCoreVisibility")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("NtpDateTime")
                        .HasColumnType("INTEGER");

                    b.Property<string>("NtpTextColor")
                        .HasColumnType("TEXT");

                    b.Property<bool>("OpSw")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("OpenTabHandel")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PipMode")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("QrCode")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ReadButton")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("RefreshButton")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ResourceSave")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SearchUrl")
                        .HasColumnType("TEXT");

                    b.Property<bool>("StatusBar")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ToolIcon")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TrackPrevention")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Translate")
                        .HasColumnType("INTEGER");

					b.Property<int>("NewTabSelectorBarVisible")
						.HasColumnType("INTEGER");

					b.Property<string>("Useragent")
                        .HasColumnType("TEXT");

					b.Property<string>("BackDrop")
					.HasColumnType("TEXT");

					b.HasKey("PackageName");

                    b.ToTable("Settings");
                });
#pragma warning restore 612, 618
        }
    }
}
