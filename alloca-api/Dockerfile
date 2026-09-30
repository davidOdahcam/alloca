# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro (cache de camadas) usando a solução e os csproj
COPY alloca-api.slnx ./
COPY src/ ./src/
RUN dotnet restore alloca-api.slnx

# Publica a API
RUN dotnet publish src/Alloca.API/Alloca.API.csproj -c Release -o /app --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Alloca.API.dll"]
