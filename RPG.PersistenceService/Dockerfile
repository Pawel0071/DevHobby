FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RPG.PersistenceService/RPG.PersistenceService.csproj", "RPG.PersistenceService/"]
COPY ["RPG.Domain/RPG.Domain.csproj", "RPG.Domain/"]
RUN dotnet restore "RPG.PersistenceService/RPG.PersistenceService.csproj"
COPY . .
WORKDIR "/src/RPG.PersistenceService"
RUN dotnet build "RPG.PersistenceService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RPG.PersistenceService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RPG.PersistenceService.dll"]
