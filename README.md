# R2SentinelBak v0.9.0

**R2SentinelBak** is a robust, multi-platform .NET 10 background service designed for automated SQL Server database backups and secure delivery to Cloudflare R2 (or any S3-compatible storage).

##  Features

- **Automated SQL Backups**: Configurable database selection via IncludedDatabases in ppsettings.json.
- **Intelligent Compression**: Backups are zipped locally using a descriptive naming convention: Backup_DB_yyyyMMdd_HHmmss.zip.
- **Cloudflare R2 Integration**: Custom Multipart Upload implementation for reliable handling of large files (>64MB), featuring R2-specific compatibility fixes (disabled payload signing and legacy checksums).
- **Resilience & Stability**:
  - Built-in retry policies using **Polly** for network and IO transients.
  - Global exception handling to prevent service crashes.
  - Automatic local cleanup of .bak and .zip files after successful uploads.
- **Cross-Platform**: Runs natively on **Ubuntu Server** and **Windows** as a standalone executable.

##  Technical Stack

- **Framework**: .NET 10.0 (Worker Service)
- **Database**: Microsoft SQL Server
- **Cloud Storage**: Cloudflare R2 / AWS S3
- **Libraries**:
  - AWSSDK.S3: For storage interaction.
  - Polly: For resilience and retry logic.
  - Serilog: For structured logging to console and local files.
  - DotNetEnv: Support for .env files in development.

##  Configuration

The service is driven by ppsettings.json or environment variables.

### Example Configuration (ppsettings.json)

`json
{
  "LogConfig": {
    "FolderPath": "Logs",
    "FileName": "log.txt",
    "MinimumLevel": "Information"
  },
  "Sentinel": {
    "R2AccessKey": "your_access_key",
    "R2SecretKey": "your_secret_key",
    "R2Endpoint": "https://<account_id>.r2.cloudflarestorage.com",
    "R2BucketName": "backups",
    "DbConnectionString": "Server=localhost;Database=master;User Id=sa;Password=your_password;TrustServerCertificate=True",
    "IncludedDatabases": [ "MainDB", "UserDB" ],
    "BackupIntervalHours": 24
  },
  "BackupSchedule": {
    "RunAtHour": 2,
    "RunAtMinute": 0
  },
  "BackupFolder": "C:\\Backups"
}
`

##  Deployment

### Standalone Executables
The project is configured to generate self-contained, single-file executables that do not require the .NET runtime installed on the host.

**Build for Windows:**
`ash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true /p:Version=0.9.0
`

**Build for Linux (Ubuntu):**
`ash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true /p:Version=0.9.0
`

##  Author

**Luciano Castillo**

---
*Disclaimer: This tool is provided "as is" for database orchestration and backup delivery.*


## 🔐 Database Security

When running as a **Windows Service**, the application defaults to the `NT AUTHORITY\SYSTEM` account, which lacks backup permissions by default (Error 916). 

For production environments, it is **strongly recommended** to use a dedicated SQL Server user instead of Integrated Security.

### Setting up a Backup User
1. Locate the script in `Scripts/CreateBackupUser.sql`.
2. Execute it on your SQL Server instance to create the `R2SentinelUser`.
3. Update your connection string in `appsettings.json`:

```json
"DbConnectionString": "Server=localhost;Database=master;User Id=R2SentinelUser;Password=YourSecurePassword123!;TrustServerCertificate=True"`n```
