FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["RPG.GameServer/RPG.GameServer.csproj", "RPG.GameServer/"]
COPY ["RPG.Application/RPG.Application.csproj", "RPG.Application/"]
COPY ["RPG.Core/RPG.Core.csproj", "RPG.Core/"]
COPY ["RPG.Abstractions/RPG.Abstractions.csproj", "RPG.Abstractions/"]
COPY ["RPG.AI/RPG.AI.csproj", "RPG.AI/"]
COPY ["RPG.Infrastructure/RPG.Infrastructure.csproj", "RPG.Infrastructure/"]
COPY ["RPG.Domain/RPG.Domain.csproj", "RPG.Domain/"]

RUN dotnet restore "RPG.GameServer/RPG.GameServer.csproj"

COPY . .
WORKDIR "/src/RPG.GameServer"
RUN dotnet build "RPG.GameServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RPG.GameServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RPG.GameServer.dll"]
