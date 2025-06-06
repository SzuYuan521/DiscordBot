# 使用 .NET 8 SDK 作為基礎映像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# 設定應用程式工作目錄
WORKDIR /app

# 開放端口 10000
EXPOSE 10000

# 設置環境變量
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

# 使用 .NET 8 SDK 來建立應用
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製 csproj 並還原相依套件
COPY ["NshmCalculator.MudClient/NshmCalculator.MudClient.csproj", "NshmCalculator.MudClient/"]
COPY ["NshmCalcuator/Shared/NshmCalcuator.Shared.csproj", "NshmCalcuator/Shared/"]
RUN dotnet restore "NshmCalculator.MudClient/NshmCalculator.MudClient.csproj"

# 複製所有文件
COPY . .

# 發佈至 publish 資料夾
WORKDIR /src/NshmCalculator.MudClient
RUN dotnet publish -c Release -o /app/publish

# 執行階段：使用 nginx 伺服器提供靜態內容
FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
