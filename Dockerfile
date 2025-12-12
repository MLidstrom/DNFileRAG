# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for restore
COPY Directory.Build.props ./
COPY src/DNFileRAG/DNFileRAG.csproj src/DNFileRAG/
COPY src/DNFileRAG.Core/DNFileRAG.Core.csproj src/DNFileRAG.Core/
COPY src/DNFileRAG.Infrastructure/DNFileRAG.Infrastructure.csproj src/DNFileRAG.Infrastructure/

# Restore dependencies (restore main project which pulls in dependencies)
RUN dotnet restore src/DNFileRAG/DNFileRAG.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/DNFileRAG/DNFileRAG.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd --gid 1000 appgroup && \
    useradd --uid 1000 --gid appgroup --shell /bin/bash --create-home appuser

# Create data directory
RUN mkdir -p /app/data/documents && \
    chown -R appuser:appgroup /app

# Copy published application
COPY --from=build /app/publish .

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Qdrant__Host=qdrant
ENV Qdrant__Port=6333

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "DNFileRAG.dll"]
