## Project Overview: R2SentinelBak

**R2SentinelBak** is a high-performance .NET 10 background service designed to orchestrate database backups, compress them using native APIs, and ship them to Cloudflare R2 storage. Built with **Vertical Slice Architecture**, it prioritizes modularity, allowing you to add new backup sources or storage destinations with zero friction.

---

## Technical Stack

* **Runtime:** .NET 10 (Targeting Native AOT for low memory footprint on VPS).
* **Architecture:** Vertical Slice Architecture (VSA).
* **Resilience:** Polly (Retry policies for R2 uploads and process execution).
* **Configuration:** DotNetEnv (.env support for R2 credentials).
* **Compression:** `System.IO.Compression` (Native .NET 10).
* **Storage:** AWSSDK.S3 (S3-compatible API for Cloudflare R2).

---

## Folder Structure

```text
R2SentinelBak/
│
├── Program.cs                        # Entry point: Host, DI, Serilog, and .env loading
├── .env                              # Secrets (R2_KEY, R2_SECRET, CONNECTION_STRING)
├── R2SentinelBak.csproj              # Project file with minimal dependencies
│
├── Features/                         # All business logic organized by slice
│   ├── SqlBackup/                    # SLICE: SQL Server logic
│   │   ├── SqlBackupHandler.cs       # Logic to invoke the backup script/process
│   │   └── BackupCommand.cs          # Data model for the backup task
│   │
│   ├── Archiving/                    # SLICE: Native ZIP compression
│   │   └── ZipService.cs             # Implements System.IO.Compression
│   │
│   ├── CloudflareR2/                 # SLICE: S3-Compatible Uploads
│   │   ├── R2ClientFactory.cs        # Configures AmazonS3Client for R2
│   │   └── Uploader.cs               # Logic for multipart/streamed uploads
│   │
│   └── Scheduling/                   # SLICE: The Watcher (Sentinel)
│       └── BackupWorker.cs           # BackgroundService with PeriodicTimer loop
│
├── Infrastructure/                   # Cross-cutting concerns
│   ├── Resilience/
│   │   └── PolicyRegistry.cs         # Polly retry/circuit-breaker definitions
│   └── Logging/
│       └── LogConfig.cs              # Serilog configuration for Console/File
│
└── Scripts/                          # External execution units
    └── DbBackup.dll                  # Your compiled C# script for raw SQL tasks

```

---

## README.md

```markdown
# R2SentinelBak 🛡️

A lightweight, production-grade .NET 10 background service designed to safeguard infrastructure data by orchestrating automated backups to Cloudflare R2.

## 🚀 Features
- **Vertical Slice Architecture**: Each feature (Backup, Compression, Upload) is self-contained.
- **Native .NET 10 Compression**: Uses the latest `System.IO.Compression` for high-speed ZIP archiving without external dependencies.
- **Polly Resilience**: Built-in retry logic for network-sensitive operations (R2 Uploads).
- **Process Orchestration**: Invokes external C# scripts as isolated processes to separate concerns.
- **Low Footprint**: Optimized for VPS environments with minimal RAM usage.

## 🛠️ Tech Stack
- **Framework**: .NET 10
- **Storage**: Cloudflare R2 (via AWS SDK S3)
- **Patterns**: BackgroundService (Worker), Vertical Slices, Result Pattern.
- **Libraries**: `Polly`, `DotNetEnv`, `Serilog`, `AWSSDK.S3`.

## 📂 Architecture
This project follows **Vertical Slice Architecture**. Instead of traditional layers (Service/Repo), logic is grouped by "Feature." This makes the code highly maintainable—if you need to change how R2 uploads work, you only touch the `Features/CloudflareR2` folder.

## ⚙️ Configuration
Create a `.env` file in the root directory:
```env
R2_ACCESS_KEY=your_key
R2_SECRET_KEY=your_secret
R2_ENDPOINT=https://<account_id>.r2.cloudflarestorage.com
R2_BUCKET_NAME=backups
DB_CONNECTION_STRING=your_sql_connection
BACKUP_INTERVAL_HOURS=24

```

## 🏗️ Getting Started

1. Clone the repo.
2. Setup your `.env` file.
3. Build the project:
```bash
dotnet build -c Release

```


4. Run the sentinel:

```bash
dotnet run --project R2SentinelBak

```



---

*Developed as a high-performance utility for infrastructure management and portfolio demonstration.*

