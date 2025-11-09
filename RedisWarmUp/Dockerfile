FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RedisWarmUp/RedisWarmUp.csproj", "RedisWarmUp/"]
RUN dotnet restore "RedisWarmUp/RedisWarmUp.csproj"
COPY . .
WORKDIR "/src/RedisWarmUp"
RUN dotnet build "RedisWarmUp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RedisWarmUp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedisWarmUp.dll"]
