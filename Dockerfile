# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Mooncake.EcommercePlatform.Domain/Mooncake.EcommercePlatform.Domain.csproj src/Mooncake.EcommercePlatform.Domain/
COPY src/Mooncake.EcommercePlatform.Application/Mooncake.EcommercePlatform.Application.csproj src/Mooncake.EcommercePlatform.Application/
COPY src/Mooncake.EcommercePlatform.Infrastructure/Mooncake.EcommercePlatform.Infrastructure.csproj src/Mooncake.EcommercePlatform.Infrastructure/
COPY src/Mooncake.EcommercePlatform.WebApi/Mooncake.EcommercePlatform.WebApi.csproj src/Mooncake.EcommercePlatform.WebApi/
RUN dotnet restore src/Mooncake.EcommercePlatform.WebApi/Mooncake.EcommercePlatform.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/Mooncake.EcommercePlatform.WebApi/Mooncake.EcommercePlatform.WebApi.csproj \
    --configuration Release --no-restore --output /publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Staging \
    Server__Port=8089 \
    TZ=Etc/UTC
COPY --from=build /publish ./
# This non-secret default is replaced by the read-only appsettings.json bind mount
# in the staging Compose stack. The real file never enters the image build.
COPY --from=build /src/src/Mooncake.EcommercePlatform.WebApi/appsettings.sample.json ./appsettings.json
RUN mkdir -p /app/logs && chown -R app:app /app
USER app
EXPOSE 8089
ENTRYPOINT ["dotnet", "Mooncake.EcommercePlatform.WebApi.dll"]
