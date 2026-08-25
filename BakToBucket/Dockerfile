FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BakToBucket.csproj", "."]
RUN dotnet restore "./BakToBucket.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "BakToBucket.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BakToBucket.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Install postgresql-client for pg_dump over the network
RUN apt-get update && \
    apt-get install -y postgresql-client && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BakToBucket.dll"]
