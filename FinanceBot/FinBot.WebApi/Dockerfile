FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FinBot.WebApi/FinBot.WebApi.csproj", "FinBot.WebApi/"]
COPY ["FinBot.Domain/FinBot.Domain.csproj", "FinBot.Domain/"]
RUN dotnet restore "FinBot.WebApi/FinBot.WebApi.csproj"
COPY . .
WORKDIR "/src/FinBot.WebApi"
RUN dotnet build "./FinBot.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FinBot.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinBot.WebApi.dll"]
