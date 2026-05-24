FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FinBot.ExcelService/FinBot.ExcelService.csproj", "FinBot.ExcelService/"]
COPY ["FinBot.Dal/FinBot.Dal.csproj", "FinBot.Dal/"]
COPY ["FinBot.Domain/FinBot.Domain.csproj", "FinBot.Domain/"]
COPY ["FinBot.MinIOS3/FinBot.MinIOS3.csproj", "FinBot.MinIOS3/"]
RUN dotnet restore "FinBot.ExcelService/FinBot.ExcelService.csproj"
COPY . .
WORKDIR "/src/FinBot.ExcelService"
RUN dotnet build "./FinBot.ExcelService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FinBot.ExcelService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinBot.ExcelService.dll"]
