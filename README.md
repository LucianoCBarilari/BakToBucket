# R2SentinelBak

R2SentinelBak is a .NET 10 scaffold for a future backup service. The current repository is intentionally minimal: it contains a basic console entry point, container metadata, and a small set of repository helpers.

## Current State

At the moment, the application entry point is still the default template:

- `Program.cs` prints `Hello, World!`
- `R2SentinelBak.csproj` targets `net10.0`
- `Dockerfile` is ready for container builds
- `docker-compose.yml` is a placeholder

This means the repository is ready for incremental development, but it does not yet implement the backup workflow described in the original draft README.

## Repository Layout

```text
R2SentinelBak/
├── README.md
├── docker-compose.yml
└── R2SentinelBak/
    ├── R2SentinelBak.slnx
    └── R2SentinelBak/
        ├── Dockerfile
        ├── Program.cs
        ├── Properties/
        │   └── launchSettings.json
        └── R2SentinelBak.csproj
```

## Requirements

- .NET 10 SDK
- Docker, if you want to build and run the container image

## Build

From the repository root:

```bash
dotnet build R2SentinelBak/R2SentinelBak/R2SentinelBak.csproj
```

## Run Locally

```bash
dotnet run --project R2SentinelBak/R2SentinelBak/R2SentinelBak.csproj
```

## Run With Docker

Build the image from the repository root:

```bash
docker build -f R2SentinelBak/R2SentinelBak/Dockerfile -t r2sentinelbak .
```

Run the container:

```bash
docker run --rm r2sentinelbak
```

## Notes

- The solution file uses the modern `.slnx` format.
- `launchSettings.json` is available for local debugging in Visual Studio and compatible tooling.
- The root `docker-compose.yml` is only a placeholder and does not define the application stack yet.
- Shell helper scripts are ignored by design, so they stay local and out of version control.

