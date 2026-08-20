FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY src/Vigie24.Domain/Vigie24.Domain.csproj ./src/Vigie24.Domain/
COPY src/Vigie24.Services/Vigie24.Services.csproj ./src/Vigie24.Services/
COPY src/Vigie24.Infrastructure/Vigie24.Infrastructure.csproj ./src/Vigie24.Infrastructure/
COPY src/Vigie24.Worker/Vigie24.Worker.csproj ./src/Vigie24.Worker/
COPY Directory.Packages.props ./

# Restore dependencies
RUN dotnet restore src/Vigie24.Worker/Vigie24.Worker.csproj

# Copy source code
COPY src/Vigie24.Domain/ ./src/Vigie24.Domain/
COPY src/Vigie24.Services/ ./src/Vigie24.Services/
COPY src/Vigie24.Infrastructure/ ./src/Vigie24.Infrastructure/
COPY src/Vigie24.Worker/ ./src/Vigie24.Worker/

# Build and publish
RUN dotnet publish src/Vigie24.Worker/Vigie24.Worker.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Vigie24.Worker.dll"]
