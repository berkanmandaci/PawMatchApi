# Stage 1: Bağımlılıkları yükle ve önbelleğe al
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece proje ve çözüm dosyalarını kopyala.
# Bu yollar build context'e (api/ klasörü) göreceli olmalıdır.
COPY ["PawMatch.sln", "."]
COPY ["PawMatch.Api/PawMatch.Api.csproj", "PawMatch.Api/"]
COPY ["PawMatch.Application/PawMatch.Application.csproj", "PawMatch.Application/"]
COPY ["PawMatch.Domain/PawMatch.Domain.csproj", "PawMatch.Domain/"]
COPY ["PawMatch.Infrastructure/PawMatch.Infrastructure.csproj", "PawMatch.Infrastructure/"]
COPY ["PawMatch.Tests/PawMatch.Tests.csproj", "PawMatch.Tests/"]

# Bu katman sadece .csproj veya .sln dosyaları değiştiğinde yeniden çalışır.
RUN dotnet restore "PawMatch.sln"

# Şimdi kodun geri kalanını kopyala
COPY . .
WORKDIR "/src/PawMatch.Api"
RUN dotnet build "PawMatch.Api.csproj" -c Release -o /app/build

# Stage 2: Uygulamayı yayınla (publish)
FROM build AS publish
RUN dotnet publish "PawMatch.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Son, küçük runtime imajını oluştur
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PawMatch.Api.dll"]

# Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=final /app/publish .
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh
# ENTRYPOINT satırını kaldırıyorum, komut docker-compose'dan alınacak.