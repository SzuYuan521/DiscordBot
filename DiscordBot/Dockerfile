# 使用 .NET 8 SDK 作為基礎映像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# 使用 .NET 8 SDK 來建立應用
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DiscordBot/DiscordBot.csproj", "DiscordBot/"]
RUN dotnet restore "DiscordBot/DiscordBot.csproj"
COPY . .
WORKDIR "/src/DiscordBot"
RUN dotnet build "DiscordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordBot.csproj" -c Release -o /app/publish

# 最終映像
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
