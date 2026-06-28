# Stage 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: SDK Build Environment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for optimized Docker caching layers
COPY ["MusicRoadmap.Api/MusicRoadmap.Api.csproj", "MusicRoadmap.Api/"]
COPY ["MusicRoadmap.Infrastructure/MusicRoadmap.Infrastructure.csproj", "MusicRoadmap.Infrastructure/"]
COPY ["MusicRoadmap.Domain/MusicRoadmap.Domain.csproj", "MusicRoadmap.Domain/"]
COPY ["MusicRoadmap.Shared/MusicRoadmap.Shared.csproj", "MusicRoadmap.Shared/"]

# Restore dependencies
RUN dotnet restore "MusicRoadmap.Api/MusicRoadmap.Api.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/MusicRoadmap.Api"
RUN dotnet build "MusicRoadmap.Api.csproj" -c Release -o /app/build

# Stage 3: Publish Binary Artifacts
FROM build AS publish
RUN dotnet publish "MusicRoadmap.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final Lean Runtime Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MusicRoadmap.Api.dll"]