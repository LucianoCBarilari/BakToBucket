# BakToBucket

Automated, multi-engine database backup service designed for secure, resilient archiving of SQL Server and PostgreSQL to Cloudflare R2 and S3-compatible storage.

![.NET](https://img.shields.io/badge/.NET-10.0%2B-blue)
![License](https://img.shields.io/badge/License-Apache_2.0-green)
![Version](https://img.shields.io/badge/Version-0.15.2-orange)

## Overview

BakToBucket is a robust, production-ready .NET 10 background service designed for zero-headache automated database backups. It safely extracts data from your database engines, validates integrity, compresses the payload, and securely delivers it to Cloudflare R2 or any S3-compatible cloud storage.

## Features

- **Multi-Engine Support:** Native orchestration for Microsoft SQL Server and PostgreSQL.
- **Backup Integrity:** Verifies SQL Server payloads using `RESTORE VERIFYONLY` prior to upload.
- **Cloud Delivery:** Direct-to-cloud transfers to Cloudflare R2 / AWS S3 with automatic bucket size threshold retention policies.
- **Docker First:** Architected for seamless deployment via Docker Compose with easy configuration mounting.
- **Resilient Operations:** Built-in network retry policies using Polly.
- **Run Once Mode:** Trigger an immediate on-demand backup cycle via CLI flag.
- **Local Only Mode:** Backup, compress, and retain archives locally without cloud transit.

## Quick Start (Docker)

The easiest and most robust way to run BakToBucket is using the official Docker image alongside your existing database containers using `docker-compose`. 

Instead of dealing with dozens of environment variables, mount your `appsettings.json` file directly into the container.

**⚠️ CRITICAL PRE-REQUISITE:** You *must* create the empty `appsettings.json` file on your host machine **before** running docker compose. If the file does not exist on disk, Docker will incorrectly create it as a directory and crash with a `500 Internal Server Error`.

```bash
# 1. Create the local archives directory on your host
mkdir -p /srv/databases/baktobucket/archives

# 2. Create the empty configuration file on your host
touch /srv/databases/baktobucket/appsettings.json
```

Now, edit your `appsettings.json` with your configuration (see the Configuration section below). To allow BakToBucket to securely connect to your other local Docker databases without complex network configurations, use the `host-gateway` bridge in your `docker-compose.yml`:

```yaml
services:
  baktobucket:
    image: ghcr.io/lucianocbarilari/baktobucket:latest
    container_name: baktobucket
    restart: always
    # Allows the container to reach the host's exposed database ports (e.g., 1433, 5432)
    extra_hosts:
      - "host.docker.internal:host-gateway"
    environment:
      # Set your timezone so scheduled backups run at the correct local time
      - TZ=America/Argentina/Buenos_Aires
    volumes:
      # If using SQL Server, mount the physical folder where SQL Server saves its .bak files
      - /srv/databases/sqlserver/backup:/app/sql_backups
      # Where your final compressed ZIPs will be saved
      - /srv/databases/baktobucket/archives:/app/Archives
      # Mount your configuration file
      - /srv/databases/baktobucket/appsettings.json:/app/appsettings.json
```

Deploy the stack:
```bash
docker compose up -d
```

## Native Installation (Windows / Linux)

If you prefer to run BakToBucket directly on the host operating system as a background daemon:

1. Download the latest release from the [Releases page](../../releases) and extract it to your server.
2. Edit `appsettings.json` with your credentials.

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

Enable and start the service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable BakToBucket
sudo systemctl start BakToBucket
```

## Configuration

The service is configured via `appsettings.json` or environment variables (using double underscore `__` for nesting).

| Section | Description |
| :--- | :--- |
| **LogOptions** | Logging configuration (folder, filename, minimum level). |
| **StorageOptions** | Cloudflare R2 / S3 storage credentials and bucket details. |
| **ConnectionStrings** | Database connection strings for `sqlserver` and `postgresql`. |
| **AppOptions** | Core application settings (`BackupHostName`, `LocalOnly`, `ZipOutputPath`, `Schedule`). |
| **Engines** | Engine-specific configs (`Enabled`, `EngineBackupPath`, `LocalBackupPath`, `IncludedDatabases`). |
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
    "sqlserver": "Server=localhost;Database=master;User Id=sa;Password=your_password;TrustServerCertificate=True",
    "postgresql": "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=your_password"
  },
  "AppOptions": {
    "BackupHostName": "Production-DB",
    "ZipOutputPath": "/app/Archives",
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
        "LocalBackupPath": "/app/sql_backups",
        "IncludedDatabases": ["ERP_Prod", "HR_Data"]
      },
      "postgresql": {
        "Enabled": true,
        "EngineBackupPath": "/app/pg_backups",
        "LocalBackupPath": "/app/pg_backups",
        "IncludedDatabases": ["WebApp_DB"]
      }
    }
  },
  "RetentionOptions": {
    "MaxBucketSizeGB": 10
  }
}
```

### SQL Server Dedicated User Setup

If you are not using `sa`, the service requires a dedicated user with backup permissions.
1. Locate the provided script: `Scripts/CreateBackupUser.sql`.
2. Execute it on your SQL Server instance to create the `BakToBucketUser`.
3. Ensure the user is mapped to each database being backed up and has `dbcreator` and `diskadmin` server roles.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
