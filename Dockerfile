# Stage 1: Build
# Bağımlılıkları yükler ve kodu derler.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece proje ve çözüm dosyalarını kopyala (önbellekleme için)
COPY ["PawMatch.sln", "."]
COPY ["PawMatch.Api/PawMatch.Api.csproj", "PawMatch.Api/"]
COPY ["PawMatch.Application/PawMatch.Application.csproj", "PawMatch.Application/"]
COPY ["PawMatch.Domain/PawMatch.Domain.csproj", "PawMatch.Domain/"]
COPY ["PawMatch.Infrastructure/PawMatch.Infrastructure.csproj", "PawMatch.Infrastructure/"]
COPY ["PawMatch.Tests/PawMatch.Tests.csproj", "PawMatch.Tests/"]

# Bağımlılıkları geri yükle. Bu adım çoğunlukla önbellekten gelir.
RUN dotnet restore "PawMatch.sln"

# Kodun geri kalanını kopyala ve build et
COPY . .
WORKDIR "/src/PawMatch.Api"
RUN dotnet build "PawMatch.Api.csproj" -c Release -o /app/build

# ----------------------------------------------------------------

# Stage 2: Publish
# Derlenmiş uygulamayı yayınlanmaya hazır hale getirir.
FROM build AS publish
WORKDIR "/src/PawMatch.Api"
RUN dotnet publish "PawMatch.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ----------------------------------------------------------------

# Stage 3: Final
# Sadece uygulamayı çalıştırmak için gerekenleri içeren küçük ve güvenli son imaj.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PawMatch.Api.dll"]
