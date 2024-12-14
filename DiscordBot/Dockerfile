# 使用 .NET 8 SDK 作為基礎映像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# 設定應用程式工作目錄
WORKDIR /app

# 開放端口 8080
EXPOSE 8080

# 設置環境變量
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# 使用 .NET 8 SDK 來建立應用
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製 csproj 並還原相依套件
COPY ["DiscordBot/DiscordBot.csproj", "DiscordBot/"]
RUN dotnet restore "DiscordBot/DiscordBot.csproj"


# 複製所有文件
COPY . .

# 設定工作目錄並建立應用
WORKDIR "/src/DiscordBot"
RUN dotnet build "DiscordBot.csproj" -c Release -o /app/build

FROM build AS publish

# 發佈應用
RUN dotnet publish "DiscordBot.csproj" -c Release -o /app/publish

# 最終映像
FROM base AS final
WORKDIR /app

# 複製發佈的應用檔案
COPY --from=publish /app/publish .

# 設定容器啟動的命令，指定應用程式名稱
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
