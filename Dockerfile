# ─── Stage 1: Build ───────────────────────────────────────────────────────────
# Usamos el SDK completo de .NET 10 para compilar y publicar la aplicación.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primero solo el .csproj para aprovechar el cache de capas de Docker.
# Si no cambian las dependencias, Docker reutiliza esta capa y no corre `dotnet restore` de nuevo.
COPY DeudoresApi/DeudoresApi.csproj DeudoresApi/
RUN dotnet restore DeudoresApi/DeudoresApi.csproj

# Copiamos el resto del código fuente y publicamos en modo Release.
COPY DeudoresApi/ DeudoresApi/
RUN dotnet publish DeudoresApi/DeudoresApi.csproj -c Release -o /app/publish --no-restore

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────────
# Usamos solo el runtime (imagen mucho más liviana, sin SDK).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copiamos únicamente el output publicado del stage anterior.
COPY --from=build /app/publish .

# El contenedor escucha en el puerto 8080 (estándar para ASP.NET Core en Docker).
EXPOSE 8080

# Variables de entorno para ASP.NET Core dentro del contenedor.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DeudoresApi.dll"]
