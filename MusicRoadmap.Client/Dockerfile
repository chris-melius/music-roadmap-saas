# Stage 1: Build static WebAssembly assets
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for caching optimization
COPY ["MusicRoadmap.Client/MusicRoadmap.Client.csproj", "MusicRoadmap.Client/"]
COPY ["MusicRoadmap.Shared/MusicRoadmap.Shared.csproj", "MusicRoadmap.Shared/"]

RUN dotnet restore "MusicRoadmap.Client/MusicRoadmap.Client.csproj"

COPY . .
WORKDIR "/src/MusicRoadmap.Client"
RUN dotnet build "MusicRoadmap.Client.csproj" -c Release -o /app/build

# Install Microsoft Library Manager CLI tool inside the build image context
RUN dotnet tool install --global Microsoft.Web.LibraryManager.Cli

# Ensure the global tools path is registered in environment memory
ENV PATH="${PATH}:/root/.dotnet/tools"

# Restore all CSS and JS client-side library dependencies cleanly before compilation!
RUN libman restore

# Publish static HTML/WASM outputs
FROM build AS publish
RUN dotnet publish "MusicRoadmap.Client.csproj" -c Release -o /app/publish

# Stage 2: Serve files using high-performance Nginx
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Clear default Nginx static assets
RUN rm -rf ./*

# Copy the compiled Blazor static wwwroot assets from stage 1
COPY --from=publish /app/publish/wwwroot .

# Copy custom Nginx configuration to support client-side Blazor routing
COPY MusicRoadmap.Client/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]