# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.13.0] - 2026-08-19

### Added
- **Multi-Engine Architecture**: BakToBucket now supports executing backups for multiple database engines (SQL Server and PostgreSQL) simultaneously or independently.
- **PostgreSQL Support**: Added `PostgreSqlBackupProvider` which executes `pg_dump` internally via `docker exec`.
- **Smart Container Auto-Discovery**: If the PostgreSQL container name isn't specified, BakToBucket will automatically discover the running Postgres container using `docker ps`.
- **Differentiated ZIP Naming**: Archive files now include the database engine name to easily distinguish them in storage (e.g., `Backup_SqlServer_...zip` vs `Backup_PostgreSql_...zip`).

### Changed
- **Configuration Structure**: Completely refactored `AppOptions` into nested configuration objects (`SqlServer` and `PostgreSql`), allowing granular control over `EngineBackupPath`, `LocalBackupPath`, and `IncludedDatabases` per engine.
- **Docker Compose Examples**: Updated documentation with modern named-volume approaches for PostgreSQL backups on Portainer.

## [0.12.1] - 2026-08-18

### Fixed
- **Docker / Linux Path Normalization**: Fixed path separator and drive letter corruption in `SqlBackupProvider` when generating backups for containerized SQL Server instances from Windows hosts (`BuildBackupFilePath`).

## [0.12.0] - 2026-08-18

### Added
- **Local-Only Backup Mode (`LocalOnly`)**: Added `LocalOnly` option to `AppOptions` allowing database backups, integrity verification (`RESTORE VERIFYONLY`), and ZIP compression to execute locally without uploading to Cloudflare R2.
- **Custom ZIP Output Path (`ZipOutputPath`)**: Configurable destination folder (absolute or relative to `LocalBackupPath`) for generated `.zip` files. When empty and `LocalOnly: true`, defaults to `<LocalBackupPath>/Archives`.
- **Conditional Diagnostics & Credential Validation**: Pre-flight R2 connectivity checks (`StartupSanityCheck`) and credential requirement validation (`StorageOptionsValidator`) are automatically bypassed when `LocalOnly` is enabled.
- **Smart Local Retention**: Retains the created `.zip` file on disk while cleaning up temporary raw `.bak` files.

### Changed
- **Renamed `BackupFolder` to `EngineBackupPath`**: Clarified configuration to represent the destination path from the perspective of the database engine (e.g., inside containerized SQL Server instances).
- **Renamed `BackupReadPath` to `LocalBackupPath`**: Clarified configuration to denote the local filesystem path where the BakToBucket service discovers, reads, and packages generated `.bak` files.
- **Updated Configuration Templates**: Applied updates across `appsettings.json`, `appsettings.Development.json`, and `.env.example`.
- **Refactored Diagnostics and Validation**: Updated `AppOptionsValidator`, `StartupSanityCheck`, and `BackupOrchestrator` to use new path identifiers.
- **Documentation**: Updated `README.md` examples and tables to reflect the new configuration property names.

## [0.11.0] - 2026-08-10

### Added
- **Run Once Mode**: Added `--run-once` CLI flag to trigger an immediate backup cycle and exit.
- **Pre-flight Sanity Checks**: Added `StartupSanityCheck` to validate database connectivity, Cloudflare R2 bucket access, and filesystem write permissions prior to service execution.
- **Storage Threshold Validation**: Added `R2BucketSizeChecker` to prevent uploads when approaching bucket capacity limits (`MaxBucketSizeGB`).
- **Resilience Pipelines**: Added Polly-based exponential backoff retry policies for Cloudflare R2 uploads (`UploadRetryPipeline`).
- **SQL Integrity Verification**: Integrated `RESTORE VERIFYONLY` post-backup execution.
- **Multiplatform Packaging**: Added GitHub Actions workflows for self-contained Linux (`linux-x64`) and Windows (`win-x64`) release binaries.
