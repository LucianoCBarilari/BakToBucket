# R2SentinelBak

Automated SQL Server backup service for secure, resilient archiving to Cloudflare R2 and S3-compatible storage.

![.NET](https://img.shields.io/badge/.NET-10.0%2B-blue)
![License](https://img.shields.io/badge/License-Apache_2.0-green)
![Version](https://img.shields.io/badge/Version-0.10.4-orange)

## Overview

R2SentinelBak is a robust, multi-platform .NET 10 background service designed for automated SQL Server database backups and secure delivery to Cloudflare R2 (or any S3-compatible storage).

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
cd /opt/r2sentinelbak
sudo wget https://github.com/<your-org>/R2SentinelBak/releases/download/v0.10.4/R2SentinelBak_v0.10.4_linux-x64.tar.gz
sudo tar -xzf R2SentinelBak_v0.10.4_linux-x64.tar.gz
sudo chmod +x R2SentinelBak
```

### Database Setup

The service requires a dedicated user with backup permissions:

1. Locate the provided script: `Scripts/CreateBackupUser.sql`.
2. Execute it on your SQL Server instance to create the `R2SentinelUser`.
3. Ensure the user is mapped to each database being backed up and has `dbcreator` and `diskadmin` server roles.
4. Use this user's credentials in your connection string.

---

## Run Once Mode

Use the `--run-once` flag to trigger an immediate backup without waiting for the scheduled time. Useful for testing your configuration or performing on-demand backups.

```bash
./R2SentinelBak --run-once
```

The service will execute the full backup cycle — backup, compress, upload to R2 — and then exit.

---

## Deployment

### Windows

```powershell
New-Service -Name "R2SentinelBak" -BinaryPathName "C:\Path\To\R2SentinelBak.exe" -DisplayName "R2 Sentinel Backup Service" -StartupType Automatic
Start-Service -Name "R2SentinelBak"
```

### Linux (Systemd)

Create a service file at `/etc/systemd/system/r2sentinelbak.service`:

```ini
[Unit]
Description=R2 Sentinel Backup Service
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/r2sentinelbak
ExecStart=/opt/r2sentinelbak/R2SentinelBak
Restart=always
RestartSec=10
User=root

[Install]
WantedBy=multi-user.target
```

Then enable and start it:

```bash
sudo systemctl daemon-reload
sudo systemctl enable r2sentinelbak
sudo systemctl start r2sentinelbak
sudo systemctl status r2sentinelbak
```

---

## SQL Server in Docker

If your SQL Server instance runs in a Docker container, the `BackupFolder` must match the **path as seen from inside the container**, not the host path.

### Why

The `BACKUP DATABASE` command is executed by SQL Server itself. It writes the `.bak` file using its own filesystem context — not the host's. If you pass a host path, SQL Server won't find it and will return `Access denied`.

### Setup

Ensure your container has a backup volume mounted in your `docker-compose.yml`:

```yaml
volumes:
  - /srv/sqlserver/backup:/var/opt/mssql/backup
```

Then configure both paths in `appsettings.json`:

```json
"AppOptions": {
  "BackupFolder": "/var/opt/mssql/backup",
  "BackupReadPath": "/srv/sqlserver/backup"
}
```

- `BackupFolder` — path **inside the container** where SQL Server writes the `.bak`
- `BackupReadPath` — path on the **host machine** where the app reads, zips, and uploads the `.bak`

If `BackupReadPath` is empty, the app falls back to `BackupFolder` — default behavior for non-Docker environments.

---

## Configuration

The service is configured via `appsettings.json` or environment variables (using double underscore `__` for nesting).

| Section | Description |
| :--- | :--- |
| **LogOptions** | Logging configuration (folder, filename, minimum level). |
| **StorageOptions** | Cloudflare R2 / S3 storage credentials and bucket details. |
| **ConnectionStrings** | Database connection strings for SqlServer and PostgreSql. |
| **AppOptions** | Core application settings (database type, backup folder, backup read path, schedule). |
| **RetentionOptions** | Maximum allowed total size for the bucket in GB. |

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
