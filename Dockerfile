# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first (cached layer: only invalidated when a csproj or package file changes).
COPY global.json .editorconfig Directory.Build.props Directory.Packages.props ./
COPY src/DddHexagonal.Domain/*.csproj src/DddHexagonal.Domain/
COPY src/DddHexagonal.Application/*.csproj src/DddHexagonal.Application/
COPY src/DddHexagonal.Infrastructure/*.csproj src/DddHexagonal.Infrastructure/
COPY src/DddHexagonal.Api/*.csproj src/DddHexagonal.Api/
RUN dotnet restore src/DddHexagonal.Api

COPY src/ src/
RUN dotnet publish src/DddHexagonal.Api -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DddHexagonal.Api.dll"]
