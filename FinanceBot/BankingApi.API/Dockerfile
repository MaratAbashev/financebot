FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BankingApi.API/BankingApi.API.csproj", "BankingApi.API/"]
COPY ["BankingApi.Persistence/BankingApi.Persistence.csproj", "BankingApi.Persistence/"]
COPY ["BankingApi.Domain/BankingApi.Domain.csproj", "BankingApi.Domain/"]
COPY ["BankingApi.Application/BankingApi.Application.csproj", "BankingApi.Application/"]
COPY ["BankingApi.Infrastructure/BankingApi.Infrastructure.csproj", "BankingApi.Infrastructure/"]
RUN dotnet restore "BankingApi.API/BankingApi.API.csproj"
COPY . .
WORKDIR "/src/BankingApi.API"
RUN dotnet build "BankingApi.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BankingApi.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BankingApi.API.dll"]
