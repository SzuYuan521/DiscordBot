# 使用 .NET 8 SDK 來建置 Blazor WASM 專案
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

# 複製發佈的 wwwroot 到 nginx html 資料夾
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# 設定 nginx 使用的 port
EXPOSE 80