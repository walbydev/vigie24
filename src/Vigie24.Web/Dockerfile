FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY src/Vigie24.Domain/Vigie24.Domain.csproj ./src/Vigie24.Domain/
COPY src/Vigie24.Services/Vigie24.Services.csproj ./src/Vigie24.Services/
COPY src/Vigie24.Infrastructure/Vigie24.Infrastructure.csproj ./src/Vigie24.Infrastructure/
COPY src/Vigie24.Web/Vigie24.Web.csproj ./src/Vigie24.Web/
COPY Directory.Packages.props ./

# Restore dependencies
RUN dotnet restore src/Vigie24.Web/Vigie24.Web.csproj

# Copy source code
COPY src/Vigie24.Domain/ ./src/Vigie24.Domain/
COPY src/Vigie24.Services/ ./src/Vigie24.Services/
COPY src/Vigie24.Infrastructure/ ./src/Vigie24.Infrastructure/
COPY src/Vigie24.Web/ ./src/Vigie24.Web/

# Build and publish
RUN dotnet publish src/Vigie24.Web/Vigie24.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy root-level appsettings (config is versioned in appsettings.*.json)
COPY appsettings*.json ./

# Copy Razor files manually (they are not included by default in publish)
COPY src/Vigie24.Web/Pages/ ./Pages/
COPY src/Vigie24.Web/Shared/ ./Shared/
COPY src/Vigie24.Web/*.razor ./

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Vigie24.Web.dll"]
