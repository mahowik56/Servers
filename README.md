# UTanks Server

This repository contains the game server implementation used for experiments. Many components are under active development.

## Requirements
- .NET 5 SDK
- MariaDB or MySQL instance

## Setup
1. Copy configuration files from `Config/` and edit them if necessary.
   Database connection settings are stored in `Config/Database.json`.
2. Restore NuGet packages and build:
   ```bash
   dotnet build UTanksServer/UTanksServer.csproj
   ```
3. Run the server:
   ```bash
   dotnet run --project UTanksServer/UTanksServer.csproj
   ```

## Deployment
Adjust the configuration files and run the server on your target machine. The static server and game server endpoints are configured via JSON files and loaded at startup.
