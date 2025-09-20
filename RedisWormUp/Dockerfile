FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cache.WormUp/Cache.WormUp.csproj", "Cache.WormUp/"]
RUN dotnet restore "Cache.WormUp/Cache.WormUp.csproj"
COPY . .
WORKDIR "/src/Cache.WormUp"
RUN dotnet build "./Cache.WormUp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cache.WormUp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cache.WormUp.dll"]
