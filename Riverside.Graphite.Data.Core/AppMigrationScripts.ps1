
#create migration script for HistoryContext
EntityFrameworkCore\Add-Migration "20240212081144_IntialCreate" -Context HistoryContext -OutputDir .\Migrations\History
#generate sql script for Context
Script-DbContext -Context DownloadContext -Output .\Riverside.Graphite.Data.Core\Schema\pragma_downloads.sql
#list the migration that are in the assembly
Migration -Context SettingsContext