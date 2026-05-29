# syntax=docker/dockerfile:1.7

#######################################################################
# Stage 1 — Build the Angular SPA into a static bundle
#######################################################################
FROM node:22-alpine AS spa-build
WORKDIR /spa

# Restore deps first so the layer caches when only sources change.
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund

# Copy the rest of the SPA and build the production bundle.
COPY frontend/ ./
RUN npx ng build --configuration production


#######################################################################
# Stage 2 — Restore + build the .NET API
#######################################################################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api-build
WORKDIR /src

# Copy only csproj files first so `dotnet restore` is cached.
COPY backend/ClaimsModule.sln ./
COPY backend/src/ClaimsModule.Domain/ClaimsModule.Domain.csproj           ./src/ClaimsModule.Domain/
COPY backend/src/ClaimsModule.Application/ClaimsModule.Application.csproj ./src/ClaimsModule.Application/
COPY backend/src/ClaimsModule.Persistence/ClaimsModule.Persistence.csproj ./src/ClaimsModule.Persistence/
COPY backend/src/ClaimsModule.Infrastructure/ClaimsModule.Infrastructure.csproj ./src/ClaimsModule.Infrastructure/
COPY backend/src/ClaimsModule.API/ClaimsModule.API.csproj                 ./src/ClaimsModule.API/
RUN dotnet restore src/ClaimsModule.API/ClaimsModule.API.csproj

# Copy the rest of the backend sources and publish.
COPY backend/. ./
RUN dotnet publish src/ClaimsModule.API/ClaimsModule.API.csproj \
    -c Release \
    -o /publish \
    /p:UseAppHost=false


#######################################################################
# Stage 3 — Runtime image: aspnet + API + Angular bundle in wwwroot
#######################################################################
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# .NET publish output (API DLLs + dependencies).
COPY --from=api-build /publish ./

# Angular static bundle goes to wwwroot so ASP.NET Core serves it.
COPY --from=spa-build /spa/dist/frontend/browser ./wwwroot

# Local-fs document storage (writable mount point — backed by a volume in docker-compose).
RUN mkdir -p /app/App_Data/uploads && chown -R app:app /app/App_Data

# Run as the non-root "app" user that the aspnet image already provides.
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -qO- http://localhost:8080/health >/dev/null 2>&1 || exit 1

ENTRYPOINT ["dotnet", "ClaimsModule.API.dll"]
