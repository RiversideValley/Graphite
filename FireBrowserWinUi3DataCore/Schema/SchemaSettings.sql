CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "IntialSettings" (
    "PackageName" TEXT NOT NULL CONSTRAINT "PK_InitSettings" PRIMARY KEY,
    "Gender" TEXT NOT NULL, 
    "DisableJavaScript" INTEGER NOT NULL,
    "DisablePassSave" INTEGER NOT NULL,
    "DisableWebMess" INTEGER NOT NULL,
    "DisableGenAutoFill" INTEGER NOT NULL,
    "ColorBackground" TEXT NULL,
    "Gender" TEXT NULL,
    "StatusBar" INTEGER NOT NULL,
    "BrowserKeys" INTEGER NOT NULL,
    "BrowserScripts" INTEGER NOT NULL,
    "Useragent" TEXT NULL,
    "LightMode" INTEGER NOT NULL,
    "OpSw" INTEGER NOT NULL,
    "EngineFriendlyName" TEXT NULL,
    "SearchUrl" TEXT NULL,
    "ColorTool" TEXT NULL,
    "ColorTV" TEXT NULL,
    "AdBlockerType" INTEGER NOT NULL,
    "Background" INTEGER NOT NULL,
    "IsAdBlockerEnabled" INTEGER NOT NULL,
    "Auto" INTEGER NOT NULL,
    "Lang" TEXT NULL,
    "ReadButton" INTEGER NOT NULL,
    "AdblockBtn" INTEGER NOT NULL,
    "Downloads" INTEGER NOT NULL,
    "Translate" INTEGER NOT NULL,
    "Favorites" INTEGER NOT NULL,
    "Historybtn" INTEGER NOT NULL,
    "QrCode" INTEGER NOT NULL,
    "FavoritesL" INTEGER NOT NULL,
    "ToolIcon" INTEGER NOT NULL,
    "DarkIcon" INTEGER NOT NULL,
    "OpenTabHandel" INTEGER NOT NULL,
    "BackButton" INTEGER NOT NULL,
    "ForwardButton" INTEGER NOT NULL,
    "RefreshButton" INTEGER NOT NULL,
    "IsLogoVisible" INTEGER NOT NULL,
    "HomeButton" INTEGER NOT NULL,
    "PipMode" INTEGER NOT NULL,
    "NtpDateTime" INTEGER NOT NULL,
    "ExitDialog" INTEGER NOT NULL,
    "NtpTextColor" TEXT NULL,
    "ExceptionLog" TEXT NULL,
    "Eq2fa" INTEGER NOT NULL,
    "Eqfav" INTEGER NOT NULL,
    "EqHis" INTEGER NOT NULL,
    "Eqsets" INTEGER NOT NULL,
    "TrackPrevention" INTEGER NOT NULL,
    "ResourceSave" INTEGER NOT NULL,
    "ConfirmCloseDlg" INTEGER NOT NULL,
    "IsFavoritesToggled" INTEGER NOT NULL,
    "IsSearchBoxToggled" INTEGER NOT NULL,
    "IsHistoryToggled" INTEGER NOT NULL,
    "IsHistoryVisible" INTEGER NOT NULL,
    "IsFavoritesVisible" INTEGER NOT NULL,
    "IsSearchVisible" INTEGER NOT NULL,
    "IsTrendingVisible" INTEGER NOT NULL,
    "NtpCoreVisibility" INTEGER NOT NULL,
    "NewTabHistoryQuick" INTEGER NOT NULL,
    "NewTabHistoryDownloads" INTEGER NOT NULL,
    "NewTabHistoryFavorites" INTEGER NOT NULL,
    "NewTabHistoryHistory" INTEGER NOT NULL,

);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240923183422_InitialSettingsSnapShot', '9.0.0-preview.7.24405.3');

COMMIT;

