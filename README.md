# BakToBucket

Automated SQL Server backup service for secure, resilient archiving to Cloudflare R2 and S3-compatible storage.

![.NET](https://img.shields.io/badge/.NET-10.0%2B-blue)
![License](https://img.shields.io/badge/License-Apache_2.0-green)
![Version](https://img.shields.io/badge/Version-0.11.0-orange)

## Overview

BakToBucket is a robust, multi-platform .NET 10 background service designed for automated SQL Server database backups and secure delivery to Cloudflare R2 (or any S3-compatible storage).

## Features

- **Automated SQL Backups:** Configurable database selection.
- **Backup Integrity:** Verified using SQL Server RESTORE VERIFYONLY.
- **Cloud Storage:** Secure delivery to Cloudflare R2 with automatic threshold checks.
- **Resilience:** Built-in retry policies using Polly.
- **Cross-Platform:** Runs on Windows and Linux (Ubuntu).
- **Run Once Mode:** Trigger an immediate backup on demand via CLI flag.

## Documentation

- **Configuration:** Settings via `appsettings.json` or environment variables.
- **Setup:** Database user configuration and service registration.

## Quick Start

### Installation

Download the latest release from the [Releases page](../../releases) and extract it to your server.

```bash
# Linux example
cd /opt/BakToBucket
sudo wget https://github.com/<your-org>/BakToBucket/releases/download/v0.11.0/BakToBucket_v0.11.0_linux-x64.tar.gz
sudo tar -xzf BakToBucket_v0.11.0_linux-x64.tar.gz
sudo chmod +x BakToBucket
```

### Database Setup

The service requires a dedicated user with backup permissions:

1. Locate the provided script: `Scripts/CreateBackupUser.sql`.
2. Execute it on your SQL Server instance to create the `BakToBucketUser`.
3. Ensure the user is mapped to each database being backed up and has `dbcreator` and `diskadmin` server roles.
4. Use this user's credentials in your connection string.

---

## Run Once Mode

Use the `--run-once` flag to trigger an immediate backup without waiting for the scheduled time. Useful for testing your configuration or performing on-demand backups.

```bash
./BakToBucket --run-once
```

The service will execute the full backup cycle — backup, compress, upload to R2 — and then exit.

---

## Deployment

### Windows

```powershell
New-Service -Name "BakToBucket" -BinaryPathName "C:\Path\To\BakToBucket.exe" -DisplayName "BakToBucket Service" -StartupType Automatic
Start-Service -Name "BakToBucket"
```

### Linux (Systemd)

Create a service file at `/etc/systemd/system/BakToBucket.service`:

```ini
[Unit]
Description=BakToBucket Service
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/BakToBucket
ExecStart=/opt/BakToBucket/BakToBucket
Restart=always
RestartSec=10
User=root

[Install]
WantedBy=multi-user.target
```

Then enable and start it:

```bash
sudo systemctl daemon-reload
sudo systemctl enable BakToBucket
sudo systemctl start BakToBucket
sudo systemctl status BakToBucket
```

---

## Configuration

The service is configured via `appsettings.json` or environment variables (using double underscore `__` for nesting).

| Section | Description |
| :--- | :--- |
| **LogOptions** | Logging configuration (folder, filename, minimum level). |
| **StorageOptions** | Cloudflare R2 / S3 storage credentials and bucket details. |
| ConnectionStrings | Database connection strings for SqlServer and PostgreSql. |
| AppOptions | Core application settings (database type, backup folder, **backup read path**, schedule). |
| RetentionOptions | Maximum allowed total size for the bucket in GB. |


### Example `appsettings.json`

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
    "BackupFolder": "/var/opt/mssql/backup",
    "BackupReadPath": "/srv/sqlserver/backup",
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

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
