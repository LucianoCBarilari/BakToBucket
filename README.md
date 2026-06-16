# R2SentinelBak v0.10.1

R2SentinelBak is a robust, multi-platform .NET 10 background service designed for automated SQL Server database backups and secure delivery to Cloudflare R2 (or any S3-compatible storage).

## Features

- Automated SQL Backups: Configurable database selection.
- Backup Integrity: Verified using SQL Server RESTORE VERIFYONLY.
- Cloud Storage: Secure delivery to Cloudflare R2 with automatic threshold checks.
- Resilience: Built-in retry policies using Polly.
- Cross-Platform: Runs on Windows and Linux (Ubuntu).

## Configuration

The service is configured via appsettings.json or environment variables (using double underscore __ for nesting).

### Structure

| Section | Description |
| :--- | :--- |
| LogOptions | Logging configuration (folder, filename, minimum level). |
| StorageOptions | Cloudflare R2 / S3 storage credentials and bucket details. |
| ConnectionStrings | Database connection strings for SqlServer and PostgreSql. |
| AppOptions | Core application settings (database type, backup folder, schedule). |
| RetentionOptions | Maximum allowed total size for the bucket in GB. |

### Example appsettings.json

```json
{
  "LogOptions": {
    "FolderPath": "Logs",
    "FileName": "log.txt",
    "MinimumLevel": "Information"
  },
  "StorageOptions": {
    "AccessKey": "your_access_key",
    "SecretKey": "your_secret_key",
    "Endpoint": "https://<account_id>.r2.cloudflarestorage.com",
    "BucketName": "backups"
  },
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=master;...",
    "PostgreSql": "Host=localhost;Database=master;..."
  },
  "AppOptions": {
    "DatabaseType": "SqlServer",
    "BackupHostName": "MyServer",
    "BackupFolder": "./Backups",
    "BackupIntervalHours": 24,
    "Schedule": {
      "RunAtHour": 2,
      "RunAtMinute": 0
    },
    "IncludedDatabases": ["Db1", "Db2"]
  },
  "RetentionOptions": {
    "MaxBucketSizeGB": 10
  }
}
```

## Setup & Installation

### 1. Database Setup
The service requires a dedicated user with backup permissions.
1. Locate the provided script: `Scripts/CreateBackupUser.sql`.
2. Execute it on your SQL Server instance to create the `R2SentinelUser`.
3. Note: The user requires the `dbcreator` and `diskadmin` server roles to perform backups and integrity verification (`RESTORE VERIFYONLY`).
4. Ensure the user is mapped to each database being backed up and has the necessary permissions within those databases.
5. Use this user's credentials in your connection string.

### 2. Deployment
Download the latest release from the repository and extract it to your server.

### 3. Service Registration

#### Windows Service
Register the application as a Windows Service using PowerShell as Administrator:
```powershell
New-Service -Name "R2SentinelBak" -BinaryPathName "C:\Path\To\R2SentinelBak.exe" -DisplayName "R2 Sentinel Backup Service" -StartupType Automatic
Start-Service -Name "R2SentinelBak"
```

#### Linux Service (Systemd)
Create a service file at `/etc/systemd/system/r2sentinelbak.service`:
```ini
[Unit]
Description=R2 Sentinel Backup Service

[Service]
ExecStart=/opt/r2sentinelbak/R2SentinelBak
WorkingDirectory=/opt/r2sentinelbak
Restart=always
User=backup-user

[Install]
WantedBy=multi-user.target
```
Then start it:
```bash
sudo systemctl daemon-reload
sudo systemctl enable r2sentinelbak
sudo systemctl start r2sentinelbak
```

## Author

Luciano Castillo

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
