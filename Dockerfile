# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish PawMatch.Api/PawMatch.Api.csproj -c Release -o /app/publish

# Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh
# ENTRYPOINT satırını kaldırıyorum, komut docker-compose'dan alınacak. 