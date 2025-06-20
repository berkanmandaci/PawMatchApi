# Stage 1: Bağımlılıkları yükle ve önbelleğe al
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece proje dosyalarını kopyala. Bunlar nadiren değişir.
COPY PawMatch.sln .
COPY api/PawMatch.Api/PawMatch.Api.csproj api/PawMatch.Api/
COPY api/PawMatch.Application/PawMatch.Application.csproj api/PawMatch.Application/
COPY api/PawMatch.Domain/PawMatch.Domain.csproj api/PawMatch.Domain/
COPY api/PawMatch.Infrastructure/PawMatch.Infrastructure.csproj api/PawMatch.Infrastructure/
# ... diğer projeleriniz varsa onlar da eklenecek ...

# Bu katman sadece .csproj dosyaları değiştiğinde yeniden çalışır.
RUN dotnet restore PawMatch.sln

# Şimdi kodun geri kalanını kopyala
COPY . .
WORKDIR "/src/api/PawMatch.Api"
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