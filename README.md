# BakToBucket

Automated SQL Server backup service for secure, resilient archiving to Cloudflare R2 and S3-compatible storage.

![.NET](https://img.shields.io/badge/.NET-10.0%2B-blue)
![License](https://img.shields.io/badge/License-Apache_2.0-green)
![Version](https://img.shields.io/badge/Version-0.13.0-orange)

## Overview

BakToBucket is a robust, multi-platform .NET 10 background service designed for automated SQL Server database backups and secure delivery to Cloudflare R2 (or any S3-compatible storage).

## Features

- **Automated SQL Backups:** Configurable database selection.
- **Backup Integrity:** Verified using SQL Server RESTORE VERIFYONLY.
- **Cloud Storage:** Secure delivery to Cloudflare R2 with automatic threshold checks.
- **Resilience:** Built-in retry policies using Polly.
- **Cross-Platform:** Runs on Windows and Linux (Ubuntu).
- **Run Once Mode:** Trigger an immediate backup on demand via CLI flag.
- **Local Only Mode:** Backup, verify integrity, and package archives locally without cloud storage upload.

## Documentation

- **Configuration:** Settings via `appsettings.json` or environment variables.
- **Setup:** Database user configuration and service registration.

## Quick Start

### Installation

Download the latest release from the [Releases page](../../releases) and extract it to your server.

```bash
# Linux example
cd /opt/BakToBucket
sudo wget https://github.com/<your-org>/BakToBucket/releases/download/v0.15.0/BakToBucket_v0.15.0_linux-x64.tar.gz
sudo tar -xzf BakToBucket_v0.15.0_linux-x64.tar.gz
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

The service will execute the full backup cycle — backup, compress, upload to R2 (or retain locally if `LocalOnly` is set) — and then exit.

---

## Deployment

### Docker (Recommended)

The easiest and most robust way to run BakToBucket is using the official Docker image alongside your databases using `docker-compose`. 

Instead of dealing with dozens of environment variables, the recommended approach is to mount your `appsettings.json` file directly into the container.

**⚠️ CRITICAL STEP:** You *must* create the empty `appsettings.json` file on your host machine **before** running docker compose. If the file does not exist, Docker will assume it's a directory and crash with a `500 Internal Server Error`.

```bash
# 1. Create the local archives directory
mkdir -p /srv/databases/baktobucket/archives

# 2. Create the empty configuration file on your host
touch /srv/databases/baktobucket/appsettings.json
```

Now, edit your `appsettings.json` with your configuration (see the Configuration section). To allow BakToBucket to securely connect to your other local Docker databases without complex network configurations, use the `host.docker.internal` trick in your `docker-compose.yml`:

```yaml
services:
  baktobucket:
    image: ghcr.io/lucianocbarilari/baktobucket:latest
    container_name: baktobucket
    restart: always
    # Allows the container to reach the host's exposed database ports
    extra_hosts:
      - "host.docker.internal:host-gateway"
    environment:
      # Ensures you can see console logs in Portainer / Docker
      - DOTNET_ENVIRONMENT=Development
      # Set your timezone so scheduled backups run at the correct local time
      - TZ=America/Argentina/Buenos_Aires
    volumes:
      # If using SQL Server, mount its backup folder
      - /srv/databases/sqlserver/backup:/app/sql_backups
      # Where your final compressed ZIPs will be saved
      - /srv/databases/baktobucket/archives:/app/Archives
      # Mount your configuration file
      - /srv/databases/baktobucket/appsettings.json:/app/appsettings.json
```

Run it with:
```bash
docker compose up -d
```

### Windows Service

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
| AppOptions | Core application settings (BackupHostName, LocalOnly, ZipOutputPath, Schedule, and engine-specific configs). |
| RetentionOptions | Maximum allowed total size for the bucket in GB. |


### Example `appsettings.json`

#### Standard Configuration

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
    "sqlserver": "Server=localhost;Database=master;...",
    "postgresql": "Host=localhost;Database=master;..."
  },
  "AppOptions": {
    "BackupHostName": "MyServer",
    "ZipOutputPath": "Archives",
    "LocalOnly": false,
    "BackupIntervalHours": 24,
    "Schedule": {
      "RunAtHour": 2,
      "RunAtMinute": 0
    },
    "Engines": {
      "sqlserver": {
        "Enabled": true,
        "EngineBackupPath": "/var/opt/mssql/backup",
        "LocalBackupPath": "/srv/sqlserver/backup",
        "IncludedDatabases": ["Db1", "Db2"]
      },
      "postgresql": {
        "Enabled": false,
        "EngineBackupPath": "/var/lib/postgresql/backup",
        "LocalBackupPath": "/srv/postgresql/backup",
        "IncludedDatabases": []
      }
    }
  },
  "RetentionOptions": {
    "MaxBucketSizeGB": 10
  }
}
```

#### SQL Server in Docker Desktop (Windows)

When running SQL Server on Docker Desktop for Windows, using a direct bind mount (e.g., `C:\Backups:/var/opt/mssql/backup`) will cause the `BACKUP DATABASE` command to fail with **OS Error 31 (DiskChangeFileSize)** due to VirtioFS/NTFS limitations.

To resolve this, use a **Docker Named Volume** and configure BakToBucket to read from the WSL2 UNC network path:

**1. `docker-compose.yml`:**
```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    volumes:
      - sql_backups:/var/opt/mssql/backup

volumes:
  sql_backups:
```

**2. `appsettings.json`:**
```json
  "AppOptions": {
    "SqlServer": {
      "Enabled": true,
      "EngineBackupPath": "/var/opt/mssql/backup",
      "LocalBackupPath": "\\\\wsl.localhost\\docker-desktop\\mnt\\docker-desktop-disk\\data\\docker\\volumes\\sqlserver_sql_backups\\_data"
    },
    "ZipOutputPath": "C:\\Backups"
  }
```
*(Note: Docker Compose usually prefixes the volume name with your project directory name, e.g., `sqlserver_sql_backups`).*

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
