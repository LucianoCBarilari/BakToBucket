# R2SentinelBak

R2SentinelBak is a .NET 10 background service scaffold for a future backup workflow. The current codebase is intentionally minimal, but the repository is set up to evolve into a production-grade sentinel for database backup orchestration and object storage delivery.

## Project Overview

The long-term vision for **R2SentinelBak** is a high-performance service that:

- orchestrates database backups,
- compresses backup artifacts using native .NET APIs,
- and ships them to Cloudflare R2 through an S3-compatible interface.

That Sentinel vision is not fully implemented yet. Today, the repository provides the base worker service, container-ready project settings, and the structure needed to grow the system incrementally.

## Current State

The application currently contains:

- a `BackgroundService` worker that runs a simple loop,
- a `Program.cs` entry point that registers the hosted service,
- a `.NET 10` worker project,
- Docker support for local container builds,
- and a basic logging configuration in `appsettings.json`.

## Technical Stack

- Framework: .NET 10
- Project type: Worker Service
- Hosting: `Microsoft.Extensions.Hosting`
- Container tooling: `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`
- Runtime target: Linux container output
- Language features: nullable reference types and implicit usings enabled

## Dependencies

The project file currently includes:

- `Microsoft.Extensions.Hosting` `10.0.8`
- `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` `1.23.0`

## Repository Layout

```text
R2SentinelBak/
├── README.md
├── docker-compose.yml
├── ghUpload.sh
├── setup.sh
└── R2SentinelBak/
    ├── R2SentinelBak.slnx
    ├── R2SentinelBak.csproj
    ├── Program.cs
    ├── Worker.cs
    ├── Dockerfile
    ├── appsettings.json
    ├── appsettings.Development.json
    └── Properties/
        └── launchSettings.json
```

## Folder Structure

```text
R2SentinelBak/
│
├── R2SentinelBak.slnx              # Solution file
├── README.md                       # Project documentation
├── docker-compose.yml              # Container composition placeholder
├── ghUpload.sh                     # Local shell helper script
├── setup.sh                        # Local shell helper script
│
└── R2SentinelBak/
    ├── Program.cs                  # Entry point: host creation and DI wiring
    ├── Worker.cs                   # BackgroundService loop
    ├── R2SentinelBak.csproj        # Worker project file
    ├── Dockerfile                  # Container build definition
    ├── appsettings.json            # Default logging configuration
    ├── appsettings.Development.json# Development overrides
    └── Properties/
        └── launchSettings.json     # Local run/debug settings
```

## Configuration

The current worker does not require external backup credentials yet. Runtime behavior is driven by the default logging configuration in `appsettings.json`.

If you extend the service toward the Sentinel backup workflow, this is where environment variables or secret-backed settings for things like database connection strings, R2 credentials, bucket names, and scheduling intervals should be documented.

## Build

From the repository root:

```bash
dotnet build R2SentinelBak/R2SentinelBak.csproj
```

## Run Locally

```bash
dotnet run --project R2SentinelBak/R2SentinelBak.csproj
```

## Run With Docker

Build the image from the repository root:

```bash
docker build -f R2SentinelBak/Dockerfile -t r2sentinelbak .
```

Run the container:

```bash
docker run --rm r2sentinelbak
```

## Sentinel Vision

When the backup pipeline is implemented, the service can grow into these slices:

- SQL backup orchestration
- native compression
- Cloudflare R2 upload
- scheduling and retry policy handling
- logging and operational resilience

Suggested future structure:

```text
Features/
├── SqlBackup/
├── Archiving/
├── CloudflareR2/
└── Scheduling/

Infrastructure/
├── Resilience/
└── Logging/

Scripts/
└── DbBackup.dll
```

## Notes

- The current codebase is a worker template, not the full Sentinel implementation.
- `docker-compose.yml` is present but still acts as a placeholder.
- The shell scripts are local helpers and may be intentionally excluded from version control depending on `.gitignore`.
